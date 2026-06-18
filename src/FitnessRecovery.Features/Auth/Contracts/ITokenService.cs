using FitnessRecovery.Features.Auth.Domain;

namespace FitnessRecovery.Features.Auth.Contracts;

public interface ITokenService
{
    (string Token, string Jti, DateTime ExpiresAt) GenerateAccessToken(User user);
    
    string GenerateRefreshToken();
}
