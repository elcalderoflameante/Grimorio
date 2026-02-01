using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Branches.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Branches.Queries;

/// <summary>
/// Handler para GetCurrentBranchQuery.
/// </summary>
public class GetCurrentBranchQueryHandler : IRequestHandler<GetCurrentBranchQuery, BranchDto?>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetCurrentBranchQueryHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<BranchDto?> Handle(GetCurrentBranchQuery request, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && !b.IsDeleted, cancellationToken);

        return branch == null ? null : _mapper.Map<BranchDto>(branch);
    }
}
