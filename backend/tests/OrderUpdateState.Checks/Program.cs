using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS;

var checks = 0;
foreach (var status in Enum.GetValues<OrderStatus>())
foreach (var expected in new bool?[] { null, true, false })
{
    var accepted = true;
    try { OrderUpdateState.ValidateExpectedDraft(status, expected); }
    catch (InvalidOperationException) { accepted = false; }
    var shouldAccept = expected == null || expected == (status == OrderStatus.Draft);
    if (accepted != shouldAccept)
        throw new Exception($"Unexpected draft guard result: {status}, expected draft: {expected}");
    checks++;
}
Console.WriteLine($"Order update state checks passed: {checks}.");
