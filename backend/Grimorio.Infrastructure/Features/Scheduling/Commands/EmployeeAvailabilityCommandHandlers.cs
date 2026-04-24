using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class AddEmployeeAvailabilityCommandHandler : IRequestHandler<AddEmployeeAvailabilityCommand, EmployeeAvailabilityDto>
{
    private readonly GrimorioDbContext _context;

    public AddEmployeeAvailabilityCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<EmployeeAvailabilityDto> Handle(AddEmployeeAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        var employeeAvailability = new EmployeeAvailability
        {
            EmployeeId = request.EmployeeId,
            UnavailableDate = request.UnavailableDate,
            Reason = request.Reason,
        };

        _context.EmployeeAvailability.Add(employeeAvailability);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeAvailabilityDto
        {
            Id = employeeAvailability.Id,
            EmployeeId = employeeAvailability.EmployeeId,
            UnavailableDate = employeeAvailability.UnavailableDate,
            Reason = employeeAvailability.Reason
        };
    }
}

public class RemoveEmployeeAvailabilityCommandHandler : IRequestHandler<RemoveEmployeeAvailabilityCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public RemoveEmployeeAvailabilityCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(RemoveEmployeeAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var employeeAvailability = await _context.EmployeeAvailability
            .FirstOrDefaultAsync(ea =>
                ea.Id == request.Id &&
                ea.EmployeeId == request.EmployeeId &&
                !ea.IsDeleted, cancellationToken);

        if (employeeAvailability == null)
            throw new InvalidOperationException("Disponibilidad no encontrada.");

        employeeAvailability.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
