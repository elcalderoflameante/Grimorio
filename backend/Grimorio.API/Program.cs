using Grimorio.Infrastructure;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Seeding;
using Grimorio.API.Notifications;
using Grimorio.API.Services;
using Grimorio.Application.Abstractions;
using Grimorio.SharedKernel.Constants;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Grimorio.API.Hubs;

// Carga variables de entorno desde .env (solo en desarrollo)
var envCandidates = new[] { "../../.env", "../.env", ".env" };
foreach (var envPath in envCandidates)
{
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        break;
    }
}

var builder = WebApplication.CreateBuilder(args);

// === Configurar Serilog ===
var logFilePath = builder.Configuration["Serilog:LogFilePath"] ?? "logs/grimorio_.txt";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// === Construir cadena de conexión desde variables de entorno ===
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? builder.Configuration["Database:Host"] ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? builder.Configuration["Database:Port"] ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? builder.Configuration["Database:Name"] ?? "grimorio_dev";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? builder.Configuration["Database:User"] ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? builder.Configuration["Database:Password"] ?? "postgres";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"] = connectionString;

// === Configurar JWT (env vars first, appsettings as fallback) ===
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY is required.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "Grimorio";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "GrimorioClient";
var jwtAccessTokenExpirationMinutes = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES")
    ?? builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"]
    ?? "480";

builder.Configuration["JwtSettings:SecretKey"] = jwtSecretKey;
builder.Configuration["JwtSettings:Issuer"] = jwtIssuer;
builder.Configuration["JwtSettings:Audience"] = jwtAudience;
builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"] = jwtAccessTokenExpirationMinutes;

var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Permite recibir el JWT desde query string para conexiones SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken)
                && path.StartsWithSegments(AppConstants.Hubs.TableServicePath))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
    };
});

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
                ?? builder.Configuration["Cors:AllowedOrigins"]
                ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            return;
        }

        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                var host = uri.Host;
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || host.Equals("127.0.0.1"))
                    return true;

                return host.StartsWith("192.168.", StringComparison.Ordinal)
                    || host.StartsWith("10.", StringComparison.Ordinal)
                    || (host.StartsWith("172.", StringComparison.Ordinal)
                        && int.TryParse(host.Split('.')[1], out var secondOctet)
                        && secondOctet >= 16 && secondOctet <= 31);
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// === Authorization handler para bypass Admin ===
builder.Services.AddSingleton<IAuthorizationHandler, Grimorio.API.Authorization.AdminBypassHandler>();

// === Authorization policies ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(AppConstants.Roles.Admin));

    // Políticas granulares por permiso — el AdminBypassHandler las bypasea para Administrador
    foreach (var permission in AppConstants.Permissions.All.Select(p => p.Code))
    {
        var captured = permission;
        options.AddPolicy(captured, policy =>
            policy.RequireAssertion(context =>
                context.User.FindAll(AppConstants.Claims.Permissions)
                    .Any(c => c.Value == captured)));
    }
});

// === Add services to the container ===
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();

// === Data Protection (cifrado del certificado .p12 y contraseña SRI) ===
var dataProtectionKeysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH")
    ?? builder.Configuration["DataProtection:KeysPath"]
    ?? (Directory.Exists("/app/secrets") ? "/app/secrets/dataprotection-keys" : null);

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("Grimorio");

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// === HTTP client para llamadas al SRI ===
builder.Services.AddHttpClient();
builder.Services.AddScoped<Grimorio.Infrastructure.Services.Sri.SriSoapClient>();
builder.Services.AddScoped<Grimorio.Infrastructure.Services.Email.IEmailService,
                           Grimorio.Infrastructure.Services.Email.SmtpEmailService>();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Grimorio.Application.Features.Auth.Commands.LoginUserCommandValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// === AutoMapper ===
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<Grimorio.Infrastructure.Mappings.MappingProfile>());

// === Registrar servicios de infraestructura ===
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
builder.Services.AddScoped<IFcmPushNotificationService, FcmPushNotificationService>();

// === Registrar MediatR ===
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Grimorio.Application.DTOs.LoginRequest).Assembly);
    config.RegisterServicesFromAssembly(typeof(Grimorio.Infrastructure.Persistence.GrimorioDbContext).Assembly);
});

var app = builder.Build();

// === Migraciones y seeding (todos los entornos) ===
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();

    // Aplica migraciones pendientes al arrancar (seguro: idempotente)
    await dbContext.Database.MigrateAsync();

    var passwordHashingService = scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();
    await AuthSeeder.SeedAsync(dbContext, passwordHashingService);
    await SchedulingSeeder.SeedSchedulingDataAsync(dbContext);

    await EnsureUserPushTokensTableAsync(dbContext);
}

// === Configure the HTTP request pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TableServiceHub>(AppConstants.Hubs.TableServicePath);
app.MapHub<KitchenHub>(AppConstants.Hubs.KitchenPath);

app.Run();

static async Task EnsureUserPushTokensTableAsync(GrimorioDbContext dbContext)
{
    const string sql = @"
CREATE SCHEMA IF NOT EXISTS auth;

CREATE TABLE IF NOT EXISTS auth.""UserPushTokens"" (
    ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
    ""UserId"" uuid NOT NULL,
    ""Token"" character varying(512) NOT NULL,
    ""Platform"" character varying(20) NOT NULL,
    ""DeviceId"" character varying(200) NULL,
    ""LastSeenAt"" timestamp with time zone NOT NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""BranchId"" uuid NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""CreatedBy"" uuid NOT NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" uuid NULL,
    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
    ""DeletedAt"" timestamp with time zone NULL,
    ""DeletedBy"" uuid NULL,
    CONSTRAINT ""PK_UserPushTokens"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_UserPushTokens_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES auth.""Users"" (""Id"") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserPushTokens_Token"" ON auth.""UserPushTokens"" (""Token"");
CREATE INDEX IF NOT EXISTS ""IX_UserPushTokens_BranchId_IsActive"" ON auth.""UserPushTokens"" (""BranchId"", ""IsActive"");
";

    await dbContext.Database.ExecuteSqlRawAsync(sql);
}
