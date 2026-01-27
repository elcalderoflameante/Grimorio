using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Grimorio.Application.DTOs;

namespace Grimorio.Infrastructure.Security;

/// <summary>
/// Servicio para generar y validar JWT tokens.
/// </summary>
public interface IJwtService
{
    string GenerateAccessToken(JwtUser user);
    string GenerateRefreshToken();
    JwtUser? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JwtSettings:SecretKey no configurado");
        _issuer = configuration["JwtSettings:Issuer"] ?? "Grimorio";
        _audience = configuration["JwtSettings:Audience"] ?? "GrimorioClient";
        _accessTokenExpirationMinutes = int.Parse(
            configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"
        );
    }

    /// <summary>
    /// Genera un JWT access token con claims del usuario.
    /// </summary>
    public string GenerateAccessToken(JwtUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("BranchId", user.BranchId.ToString()),
        };

        // Agregar roles
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Agregar permisos como array
        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un refresh token aleatorio (base64).
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Valida un JWT token y extrae sus claims.
    /// Retorna null si el token es inválido.
    /// </summary>
    public JwtUser? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var branchIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "BranchId");

            if (userIdClaim == null || branchIdClaim == null)
                return null;

            var jwtUser = new JwtUser
            {
                UserId = Guid.Parse(userIdClaim.Value),
                BranchId = Guid.Parse(branchIdClaim.Value),
                Roles = jwtToken.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                Permissions = jwtToken.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
                    .ToList()
            };

            return jwtUser;
        }
        catch
        {
            return null;
        }
    }
}
