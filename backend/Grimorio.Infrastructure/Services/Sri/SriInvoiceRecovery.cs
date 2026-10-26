using System.Xml;
using System.Xml.Linq;

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
        CancellationToken ct,
        Func<string?>? getSubmittedXml = null)
    {
        var authorization = await query();
        if (authorization.IsAuthorized) return FromAuthorization(authorization);
        if (authorization.Status == SriAuthorizationStatus.Rejected && !allowRejectedResubmission)
            return FromAuthorization(authorization, alreadyReceived, getSubmittedXml?.Invoke());
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
            if (authorization.IsAuthorized || (authorization.Status != SriAuthorizationStatus.NotFound
                && !IsPreviousRejection(authorization, getSubmittedXml?.Invoke())))
                return FromAuthorization(authorization);
            return new(SriRecoveryStatus.Rejected, null, reception.RawXml, string.Join("; ", reception.Messages));
        }

        // Persist receipt before polling: 43/70 must never trigger another submission.
        await markReceived();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0) await Task.Delay(3000, ct);
            authorization = await query();
            if (authorization.IsAuthorized || (authorization.Status == SriAuthorizationStatus.Rejected
                && !IsPreviousRejection(authorization, getSubmittedXml?.Invoke())))
                return FromAuthorization(authorization);
        }
        return FromAuthorization(authorization, true, getSubmittedXml?.Invoke());
    }

    private static SriRecoveryResult FromAuthorization(SriAuthorizationResponse authorization, bool received = false,
        string? submittedXml = null)
    {
        if (IsPreviousRejection(authorization, submittedXml))
            return new(received ? SriRecoveryStatus.Sent : SriRecoveryStatus.Pending, authorization, authorization.RawXml,
                "El SRI devuelve un rechazo de una version anterior del XML. La version corregida aun no tiene un resultado confirmado; consulte nuevamente.");
        var status = authorization.IsAuthorized ? SriRecoveryStatus.Authorized
            : authorization.Status == SriAuthorizationStatus.Rejected ? SriRecoveryStatus.Rejected
            : received || authorization.Status == SriAuthorizationStatus.Processing ? SriRecoveryStatus.Sent
            : SriRecoveryStatus.Pending;
        return new(status, authorization, authorization.RawXml,
            authorization.IsAuthorized ? null : string.Join("; ", authorization.Messages));
    }

    private static bool IsPreviousRejection(SriAuthorizationResponse authorization, string? submittedXml)
    {
        if (authorization.IsAuthorized || authorization.Status != SriAuthorizationStatus.Rejected
            || string.IsNullOrWhiteSpace(submittedXml) || string.IsNullOrWhiteSpace(authorization.XmlAuthorizado)) return false;
        try
        {
            XElement ReadInvoice(string xml)
            {
                using var reader = XmlReader.Create(new StringReader(xml.Trim()), new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8 * 1024 * 1024 });
                return XElement.Load(reader);
            }
            var current = ReadInvoice(submittedXml);
            var rejected = ReadInvoice(authorization.XmlAuthorizado);
            var key = current.Element("infoTributaria")?.Element("claveAcceso")?.Value;
            if (current.Name != "factura" || rejected.Name != "factura" || string.IsNullOrWhiteSpace(key)
                || key != rejected.Element("infoTributaria")?.Element("claveAcceso")?.Value) return false;

            // Re-signing alone is not a correction: compare invoice content, not signature IDs/timestamps.
            XNamespace signature = "http://www.w3.org/2000/09/xmldsig#";
            current.Elements(signature + "Signature").Remove();
            rejected.Elements(signature + "Signature").Remove();
            foreach (var invoice in new[] { current, rejected })
                invoice.DescendantsAndSelf().Attributes().Where(a => a.IsNamespaceDeclaration).Remove();
            return !XNode.DeepEquals(current, rejected);
        }
        catch (XmlException) { return false; }
    }
}
