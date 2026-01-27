using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Employees.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Features.Employees.Commands;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateEmployeeCommandHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Validar que la posición existe y pertenece a la rama
        var position = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId && p.BranchIdParent == request.BranchId, cancellationToken);

        if (position == null)
            throw new InvalidOperationException("La posición no existe o no pertenece a esta rama.");

        // Validar que no existe empleado con la misma cédula en la rama
        var existingEmployee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.IdentificationNumber == request.IdentificationNumber && e.BranchId == request.BranchId, cancellationToken);

        if (existingEmployee != null)
            throw new InvalidOperationException("Ya existe un empleado con este número de identificación en esta rama.");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            IdentificationNumber = request.IdentificationNumber,
            PositionId = request.PositionId,
            HireDate = request.HireDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty, // TODO: obtener del contexto de usuario
            BranchIdAssigned = request.BranchId
        };

        await _dbContext.Employees.AddAsync(employee, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar para obtener la posición
        employee = await _dbContext.Employees
            .Include(e => e.Position)
            .FirstAsync(e => e.Id == employee.Id, cancellationToken);

        var dto = _mapper.Map<EmployeeDto>(employee);
        dto.PositionName = employee.Position?.Name ?? string.Empty;
        return dto;
    }
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateEmployeeCommandHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        // Validar posición
        var position = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId && p.BranchIdParent == request.BranchId, cancellationToken);

        if (position == null)
            throw new InvalidOperationException("La posición no existe o no pertenece a esta rama.");

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.PositionId = request.PositionId;
        employee.TerminationDate = request.TerminationDate;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = Guid.Empty; // TODO: obtener del contexto de usuario

        _dbContext.Employees.Update(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar para obtener la posición actualizada
        employee = await _dbContext.Employees
            .Include(e => e.Position)
            .FirstAsync(e => e.Id == employee.Id, cancellationToken);

        var dto = _mapper.Map<EmployeeDto>(employee);
        dto.PositionName = employee.Position?.Name ?? string.Empty;
        return dto;
    }
}

public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, bool>
{
    private readonly GrimorioDbContext _dbContext;

    public DeleteEmployeeCommandHandler(GrimorioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        // Soft delete
        _dbContext.Employees.Remove(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
