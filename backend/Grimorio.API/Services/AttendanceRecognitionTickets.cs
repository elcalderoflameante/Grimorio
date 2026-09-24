using System.Security.Cryptography;

namespace Grimorio.API.Services;

/// <summary>One outstanding, short-lived recognition per kiosk; consumed atomically.</summary>
public sealed class AttendanceRecognitionTickets
{
    private readonly Dictionary<Guid, Ticket> _tickets = new();
    private readonly object _sync = new();
    private sealed record Ticket(Guid EmployeeId, string Token, DateTime ExpiresAtUtc);

    public string Issue(Guid kioskId, Guid employeeId)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            foreach (var key in _tickets.Where(x => x.Value.ExpiresAtUtc <= now).Select(x => x.Key).ToArray())
                _tickets.Remove(key);
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _tickets[kioskId] = new Ticket(employeeId, token, now.AddSeconds(30));
            return token;
        }
    }

    public bool Consume(Guid kioskId, Guid employeeId, string? token)
    {
        lock (_sync)
        {
            if (!_tickets.TryGetValue(kioskId, out var ticket) || ticket.EmployeeId != employeeId ||
                ticket.ExpiresAtUtc <= DateTime.UtcNow || ticket.Token != token) return false;
            return _tickets.Remove(kioskId);
        }
    }
}
