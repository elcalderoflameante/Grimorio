namespace Grimorio.Application.DTOs;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreatePermissionDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdatePermissionDto
{
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
