using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FitnessRecovery.Infrastructure.Authentication;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, string Jti, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var secretKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "FitnessRecovery";
        var audience = _configuration["Jwt:Audience"] ?? "FitnessRecoveryApi";
        var durationInMinutesStr = _configuration["Jwt:DurationInMinutes"] ?? "60";
        _ = double.TryParse(durationInMinutesStr, out var durationInMinutes);
        if (durationInMinutes <= 0) durationInMinutes = 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(durationInMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, jti, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
