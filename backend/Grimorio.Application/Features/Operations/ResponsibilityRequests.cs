using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Operations;

public record GetResponsibilityCatalogQuery(Guid BranchId) : IRequest<ResponsibilityCatalogDto>;
public record SaveResponsibilityNamedCommand(Guid BranchId, string Kind, ResponsibilityNamedDto Value) : IRequest<ResponsibilityNamedDto>;
public record SaveResponsibilityTaskCommand(Guid BranchId, ResponsibilityTaskDto Value) : IRequest<ResponsibilityTaskDto>;
public record SaveResponsibilityAssignmentCommand(Guid BranchId, ResponsibilityAssignmentDto Value) : IRequest<ResponsibilityAssignmentDto>;
public record GetResponsibilityDayQuery(Guid BranchId, DateOnly? Date, bool Mine) : IRequest<ResponsibilityDayDto>;
public record GenerateResponsibilitiesCommand(Guid BranchId) : IRequest;
public record CreateResponsibilityCaptureCommand(Guid BranchId, Guid ActivityId, string Phase) : IRequest<ResponsibilityCaptureDto>;
public record StoreResponsibilityEvidenceCommand(Guid BranchId, Guid CaptureId, DateTime CapturedAtUtc, string StorageKey, string Sha256, long SizeBytes) : IRequest;
public record GetResponsibilityEvidenceQuery(Guid BranchId, Guid EvidenceId, bool CanSupervise) : IRequest<string?>;
public record CompleteResponsibilityCommand(Guid BranchId, Guid ActivityId, CompleteResponsibilityDto Value) : IRequest<ResponsibilityActivityDto>;
public record GetMyResponsibilityActivityQuery(Guid BranchId, Guid ActivityId) : IRequest<ResponsibilityActivityDto>;
public record CancelResponsibilityCaptureCommand(Guid BranchId, Guid CaptureId) : IRequest;
public record TransferResponsibilityCommand(Guid BranchId, Guid ActivityId, TransferResponsibilityDto Value) : IRequest;
