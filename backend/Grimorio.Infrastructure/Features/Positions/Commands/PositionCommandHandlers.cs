using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Positions.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Features.Positions.Commands;

/// <summary>
/// Handler para CreatePositionCommand.
/// Crea una nueva posición en la rama especificada.
/// </summary>
public class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, PositionDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreatePositionCommandHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PositionDto> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        // Validar que la rama exista
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);

        if (branch == null)
            throw new InvalidOperationException("La rama especificada no existe.");

        // Validar que no exista una posición con el mismo nombre en esa rama
        var existingPosition = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.BranchIdParent == request.BranchId && p.Name == request.Name, cancellationToken);

        if (existingPosition != null)
            throw new InvalidOperationException($"Ya existe una posición con el nombre '{request.Name}' en esta rama.");

        var position = new Position
        {
            BranchIdParent = request.BranchId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = Guid.Empty, // En producción, usar el UserId del token
            BranchId = request.BranchId
        };

        _dbContext.Positions.Add(position);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PositionDto>(position);
    }
}

/// <summary>
/// Handler para UpdatePositionCommand.
/// Actualiza una posición existente.
/// </summary>
public class UpdatePositionCommandHandler : IRequestHandler<UpdatePositionCommand, PositionDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdatePositionCommandHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PositionDto> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId && p.BranchIdParent == request.BranchId, cancellationToken);

        if (position == null)
            throw new InvalidOperationException("La posición no existe.");

        // Validar unicidad del nombre
        var existingPosition = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.BranchIdParent == request.BranchId && p.Name == request.Name && p.Id != request.PositionId, cancellationToken);

        if (existingPosition != null)
            throw new InvalidOperationException($"Ya existe una posición con el nombre '{request.Name}' en esta rama.");

        position.Name = request.Name;
        position.Description = request.Description;
        position.IsActive = request.IsActive;
        position.UpdatedAt = DateTime.UtcNow;
        position.UpdatedBy = Guid.Empty; // En producción, usar el UserId del token

        _dbContext.Positions.Update(position);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PositionDto>(position);
    }
}

/// <summary>
/// Handler para DeletePositionCommand.
/// Elimina (soft delete) una posición.
/// </summary>
public class DeletePositionCommandHandler : IRequestHandler<DeletePositionCommand, bool>
{
    private readonly GrimorioDbContext _dbContext;

    public DeletePositionCommandHandler(GrimorioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeletePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId && p.BranchIdParent == request.BranchId, cancellationToken);

        if (position == null)
            throw new InvalidOperationException("La posición no existe.");

        position.IsDeleted = true;
        position.DeletedAt = DateTime.UtcNow;
        position.DeletedBy = Guid.Empty; // En producción, usar el UserId del token

        _dbContext.Positions.Update(position);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
