using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace Grimorio.Infrastructure.Services.Sri;

public enum SriSubmitResult { Received, Rejected }

public record SriValidateResponse(SriSubmitResult Result, List<string> Messages);

public record SriAuthorizationResponse(
    bool IsAuthorized,
    string? NumeroAutorizacion,
    DateTime? FechaAutorizacion,
    string? XmlAuthorizado,
    List<string> Messages);

// Cliente SOAP para los webservices del SRI Ecuador
public class SriSoapClient
{
    private readonly IHttpClientFactory _httpFactory;

    // Endpoints SRI (pruebas / producción)
    private const string RecepcionPruebas = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string RecepcionProduccion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string AutorizacionPruebas = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    private const string AutorizacionProduccion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";

    public SriSoapClient(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    // ── Enviar comprobante ─────────────────────────────────────────────────────

    public async Task<SriValidateResponse> ValidarComprobanteAsync(
        string xmlFirmado, string ambiente, CancellationToken ct = default)
    {
        var endpoint = ambiente == "2" ? RecepcionProduccion : RecepcionPruebas;
        var xmlB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlFirmado));

        var soap = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:validarComprobante>
      <xml>{xmlB64}</xml>
    </ec:validarComprobante>
  </soapenv:Body>
</soapenv:Envelope>";

        var responseXml = await PostSoapAsync(endpoint, soap, "validarComprobante", ct);
        return ParseValidarResponse(responseXml);
    }

    // ── Consultar autorización ─────────────────────────────────────────────────

    public async Task<SriAuthorizationResponse> AutorizarComprobanteAsync(
        string claveAcceso, string ambiente, CancellationToken ct = default)
    {
        var endpoint = ambiente == "2" ? AutorizacionProduccion : AutorizacionPruebas;

        var soap = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:autorizacionComprobante>
      <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
    </ec:autorizacionComprobante>
  </soapenv:Body>
</soapenv:Envelope>";

        var responseXml = await PostSoapAsync(endpoint, soap, "autorizarComprobante", ct);
        return ParseAutorizarResponse(responseXml, claveAcceso);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> PostSoapAsync(
        string endpoint, string soap, string action, CancellationToken ct)
    {
        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var content = new StringContent(soap, Encoding.UTF8, "text/xml");
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "UTF-8" };
        content.Headers.Add("SOAPAction", action);

        var response = await client.PostAsync(endpoint, content, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static SriValidateResponse ParseValidarResponse(string xml)
    {
        var doc = new XmlDocument();
        try { doc.LoadXml(xml); } catch { return new(SriSubmitResult.Rejected, ["Respuesta inválida del SRI"]); }

        var messages = new List<string>();
        var msgNodes = doc.GetElementsByTagName("mensaje");
        foreach (XmlNode n in msgNodes) messages.Add(n.InnerText);

        var estadoNodes = doc.GetElementsByTagName("estado");
        var estado = estadoNodes.Count > 0 ? estadoNodes[0]!.InnerText : "";

        var result = estado.Equals("RECIBIDA", StringComparison.OrdinalIgnoreCase)
            ? SriSubmitResult.Received
            : SriSubmitResult.Rejected;

        return new(result, messages);
    }

    private static SriAuthorizationResponse ParseAutorizarResponse(string xml, string claveAcceso)
    {
        var doc = new XmlDocument();
        try { doc.LoadXml(xml); }
        catch { return new(false, null, null, null, ["Respuesta inválida del SRI"]); }

        var messages = new List<string>();
        var msgNodes = doc.GetElementsByTagName("mensaje");
        foreach (XmlNode n in msgNodes) messages.Add(n.InnerText);

        // Buscar el nodo <autorizacion> dentro de la respuesta
        var autNodes = doc.GetElementsByTagName("autorizacion");
        if (autNodes.Count == 0)
            return new(false, null, null, null, messages.Count > 0 ? messages : ["Sin autorización del SRI"]);

        var aut = autNodes[0]!;
        var estado = aut.SelectSingleNode("estado")?.InnerText ?? "";
        var numeroAut = aut.SelectSingleNode("numeroAutorizacion")?.InnerText;
        var fechaAut = aut.SelectSingleNode("fechaAutorizacion")?.InnerText;
        var comprobante = aut.SelectSingleNode("comprobante")?.InnerText;

        bool autorizado = estado.Equals("AUTORIZADO", StringComparison.OrdinalIgnoreCase);

        DateTime? fechaDate = null;
        if (DateTime.TryParse(fechaAut, out var fd)) fechaDate = fd;

        return new(autorizado, numeroAut, fechaDate, comprobante, messages);
    }
}
