using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Operations;
using MediatR;

namespace Grimorio.Infrastructure.Features.Operations;

public class ResponsibilityHandlers(ResponsibilityService service) :
    IRequestHandler<GetResponsibilityCatalogQuery, ResponsibilityCatalogDto>,
    IRequestHandler<SaveResponsibilityNamedCommand, ResponsibilityNamedDto>,
    IRequestHandler<SaveResponsibilityTaskCommand, ResponsibilityTaskDto>,
    IRequestHandler<SaveResponsibilityAssignmentCommand, ResponsibilityAssignmentDto>,
    IRequestHandler<GetResponsibilityDayQuery, ResponsibilityDayDto>, IRequestHandler<GenerateResponsibilitiesCommand>,
    IRequestHandler<CreateResponsibilityCaptureCommand, ResponsibilityCaptureDto>, IRequestHandler<StoreResponsibilityEvidenceCommand>,
    IRequestHandler<GetResponsibilityEvidenceQuery, string?>, IRequestHandler<CompleteResponsibilityCommand, ResponsibilityActivityDto>, IRequestHandler<TransferResponsibilityCommand>,
    IRequestHandler<GetMyResponsibilityActivityQuery, ResponsibilityActivityDto>, IRequestHandler<CancelResponsibilityCaptureCommand>
{
    public Task<ResponsibilityCatalogDto> Handle(GetResponsibilityCatalogQuery r, CancellationToken ct) => service.Catalog(r.BranchId, ct);
    public Task<ResponsibilityNamedDto> Handle(SaveResponsibilityNamedCommand r, CancellationToken ct) => service.SaveNamed(r.BranchId, r.Kind, r.Value, ct);
    public Task<ResponsibilityTaskDto> Handle(SaveResponsibilityTaskCommand r, CancellationToken ct) => service.SaveTask(r.BranchId, r.Value, ct);
    public Task<ResponsibilityAssignmentDto> Handle(SaveResponsibilityAssignmentCommand r, CancellationToken ct) => service.SaveAssignment(r.BranchId, r.Value, ct);
    public Task<ResponsibilityDayDto> Handle(GetResponsibilityDayQuery r, CancellationToken ct) => service.Day(r.BranchId, r.Date, r.Mine, ct);
    public Task Handle(GenerateResponsibilitiesCommand r, CancellationToken ct) => service.Generate(r.BranchId, ct);
    public Task<ResponsibilityCaptureDto> Handle(CreateResponsibilityCaptureCommand r, CancellationToken ct) => service.Capture(r.BranchId, r.ActivityId, r.Phase, ct);
    public Task Handle(StoreResponsibilityEvidenceCommand r, CancellationToken ct) => service.StoreEvidence(r.BranchId, r.CaptureId, r.CapturedAtUtc, r.StorageKey, r.Sha256, r.SizeBytes, ct);
    public Task<string?> Handle(GetResponsibilityEvidenceQuery r, CancellationToken ct) => service.EvidencePath(r.BranchId, r.EvidenceId, r.CanSupervise, ct);
    public Task<ResponsibilityActivityDto> Handle(CompleteResponsibilityCommand r, CancellationToken ct) => service.Complete(r.BranchId, r.ActivityId, r.Value, ct);
    public Task Handle(TransferResponsibilityCommand r, CancellationToken ct) => service.Transfer(r.BranchId, r.ActivityId, r.Value, ct);
    public Task<ResponsibilityActivityDto> Handle(GetMyResponsibilityActivityQuery r, CancellationToken ct) => service.MyActivity(r.BranchId, r.ActivityId, ct);
    public Task Handle(CancelResponsibilityCaptureCommand r, CancellationToken ct) => service.CancelCapture(r.BranchId, r.CaptureId, ct);
}
