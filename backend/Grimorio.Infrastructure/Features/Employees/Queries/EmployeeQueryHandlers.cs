using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Employees.Queries;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Features.Employees.Queries;

public class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, EmployeeDto?>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetEmployeeQueryHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<EmployeeDto?> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee == null)
            return null;

        var dto = _mapper.Map<EmployeeDto>(employee);
        dto.PositionName = employee.Position?.Name ?? string.Empty;
        return dto;
    }
}

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetEmployeesQueryHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == request.BranchId);

        if (request.OnlyActive)
            query = query.Where(e => e.IsActive);

        var employees = await query
            .Include(e => e.Position)
            .OrderBy(e => e.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<EmployeeDto>>(employees);
        
        for (int i = 0; i < dtos.Count; i++)
        {
            dtos[i].PositionName = employees[i].Position?.Name ?? string.Empty;
        }

        return dtos;
    }
}
