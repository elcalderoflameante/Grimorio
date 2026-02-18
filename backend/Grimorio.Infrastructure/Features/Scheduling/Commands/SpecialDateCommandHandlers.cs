using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateSpecialDateCommandHandler : IRequestHandler<CreateSpecialDateCommand, SpecialDateDto>
{
    private readonly GrimorioDbContext _context;

    public CreateSpecialDateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateDto> Handle(CreateSpecialDateCommand request, CancellationToken cancellationToken)
    {
        // Verificar que no existe otra fecha especial en la misma fecha y rama
        var existing = await _context.SpecialDates
            .FirstOrDefaultAsync(sd => sd.BranchId == request.BranchId && sd.Date.Date == request.Date.Date && !sd.IsDeleted, cancellationToken);

        if (existing != null)
            throw new InvalidOperationException("Ya existe un día especial configurado para esta fecha en esta sucursal.");

        var specialDate = new SpecialDate
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            Date = request.Date,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.SpecialDates.Add(specialDate);
        await _context.SaveChangesAsync(cancellationToken);

        return new SpecialDateDto
        {
            Id = specialDate.Id,
            BranchId = specialDate.BranchId,
            Date = specialDate.Date,
            Name = specialDate.Name,
            Description = specialDate.Description
        };
    }
}

public class UpdateSpecialDateCommandHandler : IRequestHandler<UpdateSpecialDateCommand, SpecialDateDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateSpecialDateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateDto> Handle(UpdateSpecialDateCommand request, CancellationToken cancellationToken)
    {
        var specialDate = await _context.SpecialDates
            .FirstOrDefaultAsync(sd => sd.Id == request.Id && !sd.IsDeleted, cancellationToken);

        if (specialDate == null)
            throw new InvalidOperationException("Día especial no encontrado.");

        // Verificar que no exista otro para la misma fecha
        var existing = await _context.SpecialDates
            .FirstOrDefaultAsync(sd => sd.BranchId == specialDate.BranchId && sd.Date.Date == request.Date.Date && sd.Id != request.Id && !sd.IsDeleted, cancellationToken);

        if (existing != null)
            throw new InvalidOperationException("Ya existe un día especial configurado para esta fecha en esta sucursal.");

        specialDate.Date = request.Date;
        specialDate.Name = request.Name;
        specialDate.Description = request.Description;
        specialDate.UpdatedAt = DateTime.UtcNow;
        specialDate.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new SpecialDateDto
        {
            Id = specialDate.Id,
            BranchId = specialDate.BranchId,
            Date = specialDate.Date,
            Name = specialDate.Name,
            Description = specialDate.Description
        };
    }
}

public class DeleteSpecialDateCommandHandler : IRequestHandler<DeleteSpecialDateCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteSpecialDateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteSpecialDateCommand request, CancellationToken cancellationToken)
    {
        var specialDate = await _context.SpecialDates
            .FirstOrDefaultAsync(sd => sd.Id == request.Id && !sd.IsDeleted, cancellationToken);

        if (specialDate == null)
            throw new InvalidOperationException("Día especial no encontrado.");

        specialDate.IsDeleted = true;
        specialDate.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
