namespace Grimorio.Infrastructure.Services.Email;

public interface IEmailService
{
    /// <summary>
    /// Envía la factura electrónica por correo al cliente.
    /// Si no hay configuración SMTP activa o el cliente no tiene email, retorna silenciosamente.
    /// </summary>
    Task SendInvoiceAsync(
        Guid branchId,
        string toEmail,
        string toName,
        string numeroFactura,
        string razonSocial,
        decimal importeTotal,
        byte[] ridePdf,
        string? signedXml = null,
        CancellationToken ct = default);

    /// <summary>
    /// Envía un correo de prueba para verificar la configuración SMTP.
    /// Lanza excepción si la conexión falla.
    /// </summary>
    Task SendTestEmailAsync(Guid branchId, string toEmail, CancellationToken ct = default);
}
