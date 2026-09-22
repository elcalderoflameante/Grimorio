using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Grimorio.Infrastructure.Services.Sri;

public enum SriSubmitResult { Received, Rejected, AlreadyReceived, Unknown }
public enum SriAuthorizationStatus { Unknown, NotFound, Processing, Authorized, Rejected }

public record SriValidateResponse(SriSubmitResult Result, List<string> Messages, string RawXml);

public record SriAuthorizationResponse(
    bool IsAuthorized,
    string? NumeroAutorizacion,
    DateTime? FechaAutorizacion,
    string? XmlAuthorizado,
    List<string> Messages,
    string RawXml,
    SriAuthorizationStatus Status = SriAuthorizationStatus.Unknown);

// Cliente SOAP para los webservices del SRI Ecuador
public class SriSoapClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SriSoapClient> _log;

    // Endpoints SRI (pruebas / producción)
    private const string RecepcionPruebas = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string RecepcionProduccion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string AutorizacionPruebas = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    private const string AutorizacionProduccion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";

    public SriSoapClient(IHttpClientFactory httpFactory, ILogger<SriSoapClient> log)
    {
        _httpFactory = httpFactory;
        _log = log;
    }

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

        var responseXml = await PostSoapAsync(endpoint, soap, ct);
        _log.LogDebug("SRI validarComprobante respuesta: {Xml}", responseXml);
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

        var responseXml = await PostSoapAsync(endpoint, soap, ct);
        _log.LogDebug("SRI autorizarComprobante respuesta: {Xml}", responseXml);
        return ParseAutorizarResponse(responseXml, claveAcceso);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> PostSoapAsync(
        string endpoint, string soap, CancellationToken ct)
    {
        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        using var content = new StringContent(soap, Encoding.UTF8, "text/xml");
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "UTF-8" };
        client.DefaultRequestHeaders.Add("SOAPAction", "\"\"");

        using var response = await client.PostAsync(endpoint, content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static SriValidateResponse ParseValidarResponse(string xml)
    {
        var doc = new XmlDocument();
        try { doc.LoadXml(xml); } catch { return new(SriSubmitResult.Unknown, ["Respuesta invalida del SRI; resultado pendiente de verificar."], xml); }

        // El estado está en RespuestaRecepcionComprobante/estado
        var estado = doc.SelectSingleNode("//*[local-name()='RespuestaRecepcionComprobante']/*[local-name()='estado']")?.InnerText.Trim() ?? "";

        var messages = ExtractMensajes(doc);

        var codes = doc.SelectNodes("//*[local-name()='mensaje']/*[local-name()='identificador']")!
            .Cast<XmlNode>().Select(n => n.InnerText.Trim()).ToList();
        var result = estado.ToUpperInvariant() switch
        {
            "RECIBIDA" => SriSubmitResult.Received,
            "DEVUELTA" when codes.Any(c => c is "43" or "70") => SriSubmitResult.AlreadyReceived,
            "DEVUELTA" when codes.Contains("50") => SriSubmitResult.Unknown,
            "DEVUELTA" => SriSubmitResult.Rejected,
            _ => SriSubmitResult.Unknown,
        };

        if (messages.Count == 0 && result != SriSubmitResult.Received)
            messages.Add($"El SRI devolvió estado: {(string.IsNullOrEmpty(estado) ? "desconocido" : estado)}");

        return new(result, messages, xml);
    }

    private static SriAuthorizationResponse ParseAutorizarResponse(string xml, string claveAcceso)
    {
        var doc = new XmlDocument();
        try { doc.LoadXml(xml); }
        catch { return new(false, null, null, null, ["Respuesta inválida del SRI"], xml); }

        var response = doc.SelectSingleNode("//*[local-name()='RespuestaAutorizacionComprobante']");
        var queriedKey = response?.SelectSingleNode("*[local-name()='claveAccesoConsultada']")?.InnerText.Trim();
        if (response == null || queriedKey != claveAcceso)
            return new(false, null, null, null, ["Respuesta de autorizacion invalida o de otra clave."], xml);
        var autNodes = response.SelectNodes("*[local-name()='autorizaciones']/*[local-name()='autorizacion']")!;
        if (autNodes.Count == 0)
        {
            var messagesWithoutAuthorization = ExtractMensajes(doc);
            var count = response.SelectSingleNode("*[local-name()='numeroComprobantes']")?.InnerText.Trim();
            var state = count == "0" && messagesWithoutAuthorization.Count == 0
                ? SriAuthorizationStatus.NotFound : SriAuthorizationStatus.Unknown;
            return new(false, null, null, null,
                messagesWithoutAuthorization.Count > 0 ? messagesWithoutAuthorization : ["Sin autorizacion disponible para la clave consultada."], xml, state);
        }

        var aut = autNodes.Cast<XmlNode>().FirstOrDefault(n =>
            n.SelectSingleNode("*[local-name()='estado']")?.InnerText.Trim() == "AUTORIZADO") ?? autNodes[autNodes.Count - 1]!;
        var estado = aut.SelectSingleNode("*[local-name()='estado']")?.InnerText?.Trim() ?? "";
        var numeroAut = aut.SelectSingleNode("*[local-name()='numeroAutorizacion']")?.InnerText;
        var fechaAut = aut.SelectSingleNode("*[local-name()='fechaAutorizacion']")?.InnerText;
        var comprobante = aut.SelectSingleNode("*[local-name()='comprobante']")?.InnerText;

        bool autorizado = estado.Equals("AUTORIZADO", StringComparison.OrdinalIgnoreCase);

        DateTime? fechaDate = null;
        if (DateTime.TryParse(fechaAut, null, System.Globalization.DateTimeStyles.None, out var fd))
            fechaDate = fd.Kind == DateTimeKind.Utc ? fd : fd.ToUniversalTime();

        var messages = ExtractMensajes(doc);
        if (messages.Count == 0 && !autorizado)
            messages.Add($"El SRI devolvió estado: {(string.IsNullOrEmpty(estado) ? "desconocido" : estado)}");

        var status = estado.ToUpperInvariant() switch
        {
            "AUTORIZADO" => SriAuthorizationStatus.Authorized,
            "NO AUTORIZADO" => SriAuthorizationStatus.Rejected,
            "EN PROCESO" or "EN PROCESAMIENTO" => SriAuthorizationStatus.Processing,
            _ => SriAuthorizationStatus.Unknown,
        };
        if (autorizado && (string.IsNullOrWhiteSpace(numeroAut) || string.IsNullOrWhiteSpace(comprobante) || !fechaDate.HasValue))
            return new(false, null, null, null, ["Respuesta de autorizacion incompleta; se consultara nuevamente."], xml);
        return new(autorizado, numeroAut, fechaDate, comprobante, messages, xml, status);
    }

    // El SRI usa <mensaje> tanto como contenedor como elemento hijo con el texto.
    // Estructura: <mensajes><mensaje><identificador/><mensaje/><tipo/><informacionAdicional/></mensaje></mensajes>
    private static List<string> ExtractMensajes(XmlDocument doc)
    {
        var result = new List<string>();

        // Los nodos <mensajes> contienen los <mensaje> contenedores
        var mensajesContainers = doc.SelectNodes("//*[local-name()='mensajes']")!;
        foreach (XmlNode container in mensajesContainers)
        {
            foreach (XmlNode mensajeNode in container.ChildNodes)
            {
                if (mensajeNode.NodeType != XmlNodeType.Element) continue;

                var id = mensajeNode.SelectSingleNode("*[local-name()='identificador']")?.InnerText?.Trim();
                var texto = mensajeNode.SelectSingleNode("*[local-name()='mensaje']")?.InnerText?.Trim();
                var tipo = mensajeNode.SelectSingleNode("*[local-name()='tipo']")?.InnerText?.Trim();
                var info = mensajeNode.SelectSingleNode("*[local-name()='informacionAdicional']")?.InnerText?.Trim();

                if (string.IsNullOrEmpty(texto)) continue;

                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(id)) sb.Append($"[{id}] ");
                sb.Append(texto);
                if (!string.IsNullOrEmpty(info)) sb.Append($" — {info}");
                if (!string.IsNullOrEmpty(tipo) && tipo != "ERROR") sb.Append($" ({tipo})");

                result.Add(sb.ToString());
            }
        }

        return result;
    }
}
