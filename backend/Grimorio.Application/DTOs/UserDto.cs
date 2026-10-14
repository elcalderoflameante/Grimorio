namespace Grimorio.Application.DTOs;

public class UserDto
{
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasKdsPin { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<UserRoleDto> RoleDetails { get; set; } = new();
}

public class UserRoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class CreateUserDto
{
    public Guid? EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    private Guid? _employeeId;
    public Guid? EmployeeId
    {
        get => _employeeId;
        set { _employeeId = value; EmployeeLinkProvided = true; }
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool EmployeeLinkProvided { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AssignRolesDto
{
    public List<string> RoleIds { get; set; } = new();
}

public class SetKdsPinDto
{
    public string? Pin { get; set; }
}

public class UserEmployeeOptionDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? LinkedUserId { get; set; }
}
