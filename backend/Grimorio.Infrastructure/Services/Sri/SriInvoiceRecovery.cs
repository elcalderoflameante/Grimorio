namespace Grimorio.Infrastructure.Services.Sri;

public enum SriRecoveryStatus { Pending, Sent, Authorized, Rejected }
public record SriRecoveryResult(SriRecoveryStatus Status, SriAuthorizationResponse? Authorization, string RawXml, string? Error);

public static class SriInvoiceRecovery
{
    public static async Task<SriRecoveryResult> ProcessAsync(
        Func<Task<SriAuthorizationResponse>> query,
        Func<Task<SriValidateResponse>> submit,
        Func<Task> markReceived,
        bool alreadyReceived,
        bool allowRejectedResubmission,
        CancellationToken ct)
    {
        var authorization = await query();
        if (authorization.IsAuthorized) return FromAuthorization(authorization);
        if (authorization.Status == SriAuthorizationStatus.Rejected && !allowRejectedResubmission)
            return FromAuthorization(authorization);
        if (authorization.Status is not (SriAuthorizationStatus.NotFound or SriAuthorizationStatus.Rejected))
            return FromAuthorization(authorization, alreadyReceived);
        if (alreadyReceived && authorization.Status == SriAuthorizationStatus.NotFound)
            return FromAuthorization(authorization, true);

        var reception = await submit();
        if (reception.Result == SriSubmitResult.Unknown)
            return new(SriRecoveryStatus.Pending, null, reception.RawXml, string.Join("; ", reception.Messages));

        if (reception.Result == SriSubmitResult.Rejected)
        {
            // A lost earlier response can surface as a reception error. Reconcile first.
            authorization = await query();
            if (authorization.IsAuthorized || authorization.Status != SriAuthorizationStatus.NotFound)
                return FromAuthorization(authorization);
            return new(SriRecoveryStatus.Rejected, null, reception.RawXml, string.Join("; ", reception.Messages));
        }

        // Persist receipt before polling: 43/70 must never trigger another submission.
        await markReceived();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0) await Task.Delay(3000, ct);
            authorization = await query();
            if (authorization.IsAuthorized || authorization.Status == SriAuthorizationStatus.Rejected)
                return FromAuthorization(authorization);
        }
        return FromAuthorization(authorization, true);
    }

    private static SriRecoveryResult FromAuthorization(SriAuthorizationResponse authorization, bool received = false)
    {
        var status = authorization.IsAuthorized ? SriRecoveryStatus.Authorized
            : authorization.Status == SriAuthorizationStatus.Rejected ? SriRecoveryStatus.Rejected
            : received || authorization.Status == SriAuthorizationStatus.Processing ? SriRecoveryStatus.Sent
            : SriRecoveryStatus.Pending;
        return new(status, authorization, authorization.RawXml,
            authorization.IsAuthorized ? null : string.Join("; ", authorization.Messages));
    }
}
