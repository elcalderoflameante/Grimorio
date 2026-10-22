using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Attendance;

public static class AttendanceEmployeeLock
{
    public static Task AcquireAsync(GrimorioDbContext context, Guid employeeId, CancellationToken ct) =>
        AcquireKeyAsync(context, "attendance-employee:" + employeeId, ct);

    public static Task AcquireKeyAsync(GrimorioDbContext context, string key, CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Attendance locks require a transaction.");
        var lockId = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
    }
}
