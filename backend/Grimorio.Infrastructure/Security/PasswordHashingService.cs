namespace Grimorio.Infrastructure.Security;

/// <summary>
/// Servicio para hash y verificación segura de contraseñas.
/// Usa bcrypt internamente.
/// </summary>
public interface IPasswordHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordHashingService : IPasswordHashingService
{
    /// <summary>
    /// Genera un hash seguro de la contraseña usando BCrypt.
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifica que una contraseña coincida con su hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
