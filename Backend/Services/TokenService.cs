using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public class TokenService(IConfiguration configuration)
{
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    private readonly string _audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
    private readonly SymmetricSecurityKey _signingKey = new(
        Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.")))
    {
        KeyId = "job-application-tracker-signing-key",
    };
    private readonly int _expiryMinutes =
        int.TryParse(configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60;

    public (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
