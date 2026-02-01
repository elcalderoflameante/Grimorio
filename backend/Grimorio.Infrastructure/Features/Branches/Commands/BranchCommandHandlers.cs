using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Branches.Commands;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Branches.Commands;

/// <summary>
/// Handler para UpdateBranchCommand.
/// </summary>
public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateBranchCommandHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<BranchDto> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && !b.IsDeleted, cancellationToken);

        if (branch == null)
            throw new InvalidOperationException("La sucursal no existe.");

        branch.Name = request.Name;
        branch.Code = request.Code;
        branch.Address = request.Address;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.IsActive = request.IsActive;
        branch.Latitude = request.Latitude;
        branch.Longitude = request.Longitude;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = Guid.Empty; // En producción, usar UserId del token

        _dbContext.Branches.Update(branch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BranchDto>(branch);
    }
}
