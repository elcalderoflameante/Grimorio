using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Attendance;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Services;

public sealed class AttendanceOperationExecutor(GrimorioDbContext context, AttendanceRecognitionTickets tickets)
{
    public async Task<AttendanceStatusDto> ExecuteAsync(Guid operationId, Guid branchId, Guid kioskId,
        Guid employeeId, string action, string? token, string? evidence,
        Func<CancellationToken, Task<AttendanceStatusDto>> mark, CancellationToken ct)
    {
        if (operationId == Guid.Empty) throw new InvalidOperationException("El identificador de operación no es válido.");
        var hash = Fingerprint(kioskId, employeeId, action, token, evidence);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await AttendanceEmployeeLock.AcquireKeyAsync(context, "attendance-operation:" + operationId, ct);
        var previous = await context.AttendanceKioskOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId, ct);
        if (previous is not null)
        {
            if (previous.BranchId != branchId || previous.KioskDeviceId != kioskId ||
                previous.EmployeeId != employeeId || previous.RequestHash != hash)
                throw new InvalidOperationException("El identificador de operación pertenece a otra marcación.");
            return JsonSerializer.Deserialize<AttendanceStatusDto>(previous.ResponseJson)
                ?? throw new InvalidOperationException("No se pudo recuperar el resultado de la marcación.");
        }
        await AttendanceEmployeeLock.AcquireAsync(context, employeeId, ct);
        if (!tickets.Consume(kioskId, employeeId, token))
            throw new InvalidOperationException("Identifica nuevamente tu rostro para registrar la marcación.");
        var result = await mark(ct);
        context.AttendanceKioskOperations.Add(new AttendanceKioskOperation
        {
            Id = operationId, BranchId = branchId, KioskDeviceId = kioskId, EmployeeId = employeeId,
            RequestHash = hash, ResponseJson = JsonSerializer.Serialize(result)
        });
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public static string Fingerprint(Guid kioskId, Guid employeeId, string action, string? token, string? evidence) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { kioskId, employeeId, action, token, evidence }))));
}
