using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateScheduleConfigurationCommandHandler : IRequestHandler<CreateScheduleConfigurationCommand, ScheduleConfigurationDto>
{
    private readonly GrimorioDbContext _context;

    public CreateScheduleConfigurationCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ScheduleConfigurationDto> Handle(CreateScheduleConfigurationCommand request, CancellationToken cancellationToken)
    {
        var existingConfig = await _context.ScheduleConfigurations
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        if (existingConfig != null)
            throw new InvalidOperationException("Ya existe una configuración de horarios para esta sucursal.");

        var config = new ScheduleConfiguration
        {
            BranchId = request.BranchId,
            HoursPerDay = request.HoursPerDay,
            FreeDayColor = string.IsNullOrWhiteSpace(request.FreeDayColor)
                ? AppConstants.Scheduling.DefaultFreeDayColor
                : request.FreeDayColor,
        };

        _context.ScheduleConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static ScheduleConfigurationDto MapToDto(ScheduleConfiguration config) => new()
    {
        Id = config.Id,
        BranchId = config.BranchId,
        HoursPerDay = config.HoursPerDay,
        FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor)
            ? AppConstants.Scheduling.DefaultFreeDayColor
            : config.FreeDayColor
    };
}

public class UpdateScheduleConfigurationCommandHandler : IRequestHandler<UpdateScheduleConfigurationCommand, ScheduleConfigurationDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateScheduleConfigurationCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ScheduleConfigurationDto> Handle(UpdateScheduleConfigurationCommand request, CancellationToken cancellationToken)
    {
        var config = await _context.ScheduleConfigurations
            .FirstOrDefaultAsync(sc => sc.Id == request.Id && !sc.IsDeleted, cancellationToken);

        if (config == null)
            throw new InvalidOperationException("Configuración de horarios no encontrada.");

        config.HoursPerDay = request.HoursPerDay;
        config.FreeDayColor = string.IsNullOrWhiteSpace(request.FreeDayColor)
            ? AppConstants.Scheduling.DefaultFreeDayColor
            : request.FreeDayColor;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static ScheduleConfigurationDto MapToDto(ScheduleConfiguration config) => new()
    {
        Id = config.Id,
        BranchId = config.BranchId,
        HoursPerDay = config.HoursPerDay,
        FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor)
            ? AppConstants.Scheduling.DefaultFreeDayColor
            : config.FreeDayColor
    };
}
