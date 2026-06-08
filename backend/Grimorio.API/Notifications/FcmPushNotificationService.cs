using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Grimorio.Application.DTOs;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Notifications;

public interface IFcmPushNotificationService
{
    Task<int> SendNewTableRequestAsync(TableServiceRequestDto request, CancellationToken cancellationToken = default);
}

public class FcmPushNotificationService : IFcmPushNotificationService
{
    private static readonly object FirebaseInitLock = new();

    private readonly GrimorioDbContext _dbContext;
    private readonly ILogger<FcmPushNotificationService> _logger;
    private readonly FirebaseMessaging? _messaging;

    public FcmPushNotificationService(
        GrimorioDbContext dbContext,
        IConfiguration configuration,
        ILogger<FcmPushNotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _messaging = CreateMessagingClient(configuration, logger);
    }

    public async Task<int> SendNewTableRequestAsync(TableServiceRequestDto request, CancellationToken cancellationToken = default)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Skipping FCM send because messaging client is not initialized.");
            return 0;
        }

        var tokens = await _dbContext.UserPushTokens
            .AsNoTracking()
            .Where(t => t.BranchId == request.BranchId && t.IsActive && !t.IsDeleted)
            .Select(t => t.Token)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            _logger.LogInformation("Skipping FCM send because there are no active tokens for Branch={BranchId}.", request.BranchId);
            return 0;
        }

        var tableLabel = string.IsNullOrWhiteSpace(request.TableArea)
            ? $"Mesa {request.TableCode}"
            : $"Mesa {request.TableCode} ({request.TableArea})";
        var title = $"Nueva solicitud - {tableLabel}";
        var body = string.IsNullOrWhiteSpace(request.CustomMessage)
            ? request.Type.ToString()
            : request.CustomMessage!;

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = title,
                Body = body,
            },
            Data = new Dictionary<string, string>
            {
                ["event"] = "tableService:new-request",
                ["requestId"] = request.Id.ToString(),
                ["tableCode"] = request.TableCode,
                ["tableArea"] = request.TableArea ?? string.Empty,
                ["type"] = ((int)request.Type).ToString(),
                ["customMessage"] = request.CustomMessage ?? string.Empty,
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "table_requests_channel",
                    Priority = NotificationPriority.MAX,
                    DefaultSound = true,
                    DefaultVibrateTimings = true,
                    Visibility = NotificationVisibility.PUBLIC,
                },
            },
        };

        var response = await _messaging.SendEachForMulticastAsync(message, cancellationToken);

        _logger.LogInformation(
            "FCM multicast sent. Success={SuccessCount}, Failure={FailureCount}, Branch={BranchId}, Tokens={TokenCount}",
            response.SuccessCount,
            response.FailureCount,
            request.BranchId,
            tokens.Count);

        if (response.FailureCount > 0)
        {
            _logger.LogWarning(
                "FCM sent with partial failures. Success={SuccessCount}, Failure={FailureCount}, Branch={BranchId}",
                response.SuccessCount,
                response.FailureCount,
                request.BranchId);
        }

        return response.SuccessCount;
    }

    private static FirebaseMessaging? CreateMessagingClient(
        IConfiguration configuration,
        ILogger<FcmPushNotificationService> logger)
    {
        var credentialsPath = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_PATH")
            ?? configuration["Firebase:ServiceAccountPath"];

        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            logger.LogWarning(
                "FCM disabled. Set FIREBASE_SERVICE_ACCOUNT_PATH (or Firebase:ServiceAccountPath) with a valid JSON credentials file.");
            return null;
        }

        const string appName = "grimorio-fcm";
        FirebaseApp app;

        lock (FirebaseInitLock)
        {
            var existingApp = FirebaseApp.GetInstance(appName);
            if (existingApp != null)
            {
                app = existingApp;
            }
            else
            {
                try
                {
                    app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialsPath),
                    }, appName);
                }
                catch (ArgumentException)
                {
                    // If another request created the same named app concurrently, reuse it.
                    app = FirebaseApp.GetInstance(appName)!;
                }
            }
        }

        return FirebaseMessaging.GetMessaging(app);
    }
}
