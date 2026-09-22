using System.Net;
using System.Text;
using Grimorio.Infrastructure.Services.Sri;
using Microsoft.Extensions.Logging.Abstractions;

var checks = 0;
var key = new string('1', 49);
void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception(scenario);
    checks++;
}
SriSoapClient Client(string xml) => new(new FakeFactory(xml), NullLogger<SriSoapClient>.Instance);
string AuthorizationXml(string? state, string? queriedKey = null) =>
    $"<s:RespuestaAutorizacionComprobante xmlns:s='urn:test'><s:claveAccesoConsultada>{queriedKey ?? key}</s:claveAccesoConsultada>"
    + $"<s:numeroComprobantes>{(state == null ? 0 : 1)}</s:numeroComprobantes><s:autorizaciones>"
    + (state == null ? "" : $"<s:autorizacion><s:estado>{state}</s:estado><s:numeroAutorizacion>{key}</s:numeroAutorizacion>"
        + "<s:fechaAutorizacion>2026-09-22T12:00:00-05:00</s:fechaAutorizacion><s:comprobante><![CDATA[<factura/>]]></s:comprobante></s:autorizacion>")
    + "</s:autorizaciones></s:RespuestaAutorizacionComprobante>";

foreach (var (state, expected) in new (string?, SriAuthorizationStatus)[]
{
    (null, SriAuthorizationStatus.NotFound), ("AUTORIZADO", SriAuthorizationStatus.Authorized),
    ("NO AUTORIZADO", SriAuthorizationStatus.Rejected), ("EN PROCESO", SriAuthorizationStatus.Processing),
    ("NUEVO ESTADO", SriAuthorizationStatus.Unknown),
})
{
    var response = await Client(AuthorizationXml(state)).AutorizarComprobanteAsync(key, "1");
    Check(response.Status == expected, $"Parse authorization {state}");
}
foreach (var xml in new[] { "not XML", "<Fault/>", "<RespuestaAutorizacionComprobante><numeroComprobantes>0</numeroComprobantes></RespuestaAutorizacionComprobante>", AuthorizationXml(null, "another-key") })
    Check((await Client(xml).AutorizarComprobanteAsync(key, "1")).Status == SriAuthorizationStatus.Unknown, "Invalid response must not permit sending");

foreach (var (code, expected) in new[] { ("43", SriSubmitResult.AlreadyReceived), ("70", SriSubmitResult.AlreadyReceived), ("35", SriSubmitResult.Rejected), ("50", SriSubmitResult.Unknown) })
{
    var xml = $"<RespuestaRecepcionComprobante><estado>DEVUELTA</estado><mensajes><mensaje><identificador>{code}</identificador><mensaje>Error</mensaje></mensaje></mensajes></RespuestaRecepcionComprobante>";
    Check((await Client(xml).ValidarComprobanteAsync("<factura/>", "1")).Result == expected, $"Reception {code}");
}
Check((await Client("bad XML").ValidarComprobanteAsync("<factura/>", "1")).Result == SriSubmitResult.Unknown, "Invalid reception is technical");

SriAuthorizationResponse Response(SriAuthorizationStatus state) => new(
    state == SriAuthorizationStatus.Authorized, key, DateTime.UtcNow, "<factura/>", [], "<response/>", state);
async Task Scenario(SriAuthorizationStatus[] responses, SriSubmitResult reception, bool received, bool manual,
    SriRecoveryStatus expected, int expectedSubmissions, int expectedReceipts)
{
    var queue = new Queue<SriAuthorizationStatus>(responses);
    var submissions = 0;
    var receipts = 0;
    var result = await SriInvoiceRecovery.ProcessAsync(
        () => Task.FromResult(Response(queue.Dequeue())),
        () => { submissions++; return Task.FromResult(new SriValidateResponse(reception, ["Reception message"], "<reception/>")); },
        () => { receipts++; return Task.CompletedTask; }, received, manual, CancellationToken.None);
    Check(result.Status == expected, $"Recovery expected {expected}, got {result.Status}");
    Check(submissions == expectedSubmissions, "Unexpected retransmission");
    Check(receipts == expectedReceipts, "Receipt must be persisted before authorization polling");
}
await Scenario([SriAuthorizationStatus.Authorized], SriSubmitResult.Received, false, false, SriRecoveryStatus.Authorized, 0, 0);
await Scenario([SriAuthorizationStatus.NotFound], SriSubmitResult.Received, true, false, SriRecoveryStatus.Sent, 0, 0);
await Scenario([SriAuthorizationStatus.Unknown], SriSubmitResult.Received, false, false, SriRecoveryStatus.Pending, 0, 0);
await Scenario([SriAuthorizationStatus.Processing], SriSubmitResult.Received, false, false, SriRecoveryStatus.Sent, 0, 0);
await Scenario([SriAuthorizationStatus.NotFound, SriAuthorizationStatus.Authorized], SriSubmitResult.AlreadyReceived, false, false, SriRecoveryStatus.Authorized, 1, 1);
await Scenario([SriAuthorizationStatus.NotFound, SriAuthorizationStatus.Authorized], SriSubmitResult.Rejected, false, false, SriRecoveryStatus.Authorized, 1, 0);
await Scenario([SriAuthorizationStatus.NotFound, SriAuthorizationStatus.NotFound], SriSubmitResult.Rejected, false, false, SriRecoveryStatus.Rejected, 1, 0);
await Scenario([SriAuthorizationStatus.NotFound], SriSubmitResult.Unknown, false, false, SriRecoveryStatus.Pending, 1, 0);
await Scenario([SriAuthorizationStatus.Rejected], SriSubmitResult.Received, true, false, SriRecoveryStatus.Rejected, 0, 0);
await Scenario([SriAuthorizationStatus.Rejected, SriAuthorizationStatus.Authorized], SriSubmitResult.Received, false, true, SriRecoveryStatus.Authorized, 1, 1);
var sentAfterQueryFailure = false;
try
{
    await SriInvoiceRecovery.ProcessAsync(
        () => throw new HttpRequestException("Lost response"),
        () => { sentAfterQueryFailure = true; throw new Exception("Must not submit"); },
        () => Task.CompletedTask, false, false, CancellationToken.None);
    throw new Exception("Expected network failure");
}
catch (HttpRequestException) { Check(!sentAfterQueryFailure, "Do not submit when query failed"); }
Console.WriteLine($"SRI recovery checks passed: {checks}. No network or database used.");

sealed class FakeFactory(string xml) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new FakeHandler(xml));
}
sealed class FakeHandler(string xml) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml, Encoding.UTF8, "text/xml") });
}
