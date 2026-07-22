using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Auth.Commands;

public class KdsLoginCommand : IRequest<AuthResponse>
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string Pin { get; set; } = string.Empty;
    public bool RequireWaitstaffRole { get; set; }
}
