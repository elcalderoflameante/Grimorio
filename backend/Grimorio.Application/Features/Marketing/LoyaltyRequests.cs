using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Marketing;

public record GetLoyaltyProgramQuery(Guid BranchId) : IRequest<LoyaltyProgramDto>;
public record SaveLoyaltyProgramCommand(Guid BranchId, LoyaltyProgramDto Program) : IRequest<LoyaltyProgramDto>;
public record GetLoyaltyMembersQuery(Guid BranchId, string? Search = null) : IRequest<List<LoyaltyMemberDto>>;
public record EnrollLoyaltyMemberCommand(Guid BranchId, EnrollLoyaltyMemberDto Member) : IRequest<LoyaltyMemberDetailDto>;
public record UpdateLoyaltyMemberCommand(Guid BranchId, Guid MemberId, LoyaltyMemberProfileDto Member) : IRequest<LoyaltyMemberDetailDto>;
public record GetLoyaltyMemberQuery(Guid BranchId, Guid MemberId) : IRequest<LoyaltyMemberDetailDto?>;
public record GetPublicLoyaltyCardQuery(string Token) : IRequest<PublicLoyaltyCardDto?>;
public record GetLoyaltySummaryQuery(Guid BranchId) : IRequest<LoyaltySummaryDto>;
