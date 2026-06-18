namespace FitnessRecovery.Features.Auth.Contracts;

public interface ITokenCacheService
{
    Task BlacklistAccessTokenAsync(string jti, TimeSpan expiration);
    
    Task<bool> IsAccessTokenBlacklistedAsync(string jti);
    
    Task CacheRefreshTokenAsync(string token, Guid userId, TimeSpan expiration);
    
    Task<Guid?> GetCachedRefreshTokenUserIdAsync(string token);
    
    Task InvalidateRefreshTokenAsync(string token);
}
