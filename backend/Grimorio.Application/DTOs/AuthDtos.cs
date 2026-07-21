namespace Grimorio.Application.DTOs;

/// <summary>
/// Solicitud de login del usuario.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de autenticación con JWT y refresh token.
/// </summary>
public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Solicitud para refrescar el JWT usando un refresh token.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class KdsBranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class KdsUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool HasKdsPin { get; set; }
}

public class KdsLoginRequest
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string Pin { get; set; } = string.Empty;
}

/// <summary>
/// Información del usuario del JWT (claims).
/// </summary>
public class JwtUser
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
