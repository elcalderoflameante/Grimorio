namespace Grimorio.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }
}
