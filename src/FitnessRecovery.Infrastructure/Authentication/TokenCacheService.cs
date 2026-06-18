using FitnessRecovery.Features.Auth.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace FitnessRecovery.Infrastructure.Authentication;

public class TokenCacheService : ITokenCacheService
{
    private readonly IDistributedCache _cache;
    private const string BlacklistPrefix = "blacklist:jti:";
    private const string RefreshTokenPrefix = "refresh:token:";

    public TokenCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task BlacklistAccessTokenAsync(string jti, TimeSpan expiration)
    {
        var key = $"{BlacklistPrefix}{jti}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, "revoked", options);
    }

    public async Task<bool> IsAccessTokenBlacklistedAsync(string jti)
    {
        var key = $"{BlacklistPrefix}{jti}";
        var val = await _cache.GetStringAsync(key);
        return val != null;
    }

    public async Task CacheRefreshTokenAsync(string token, Guid userId, TimeSpan expiration)
    {
        var key = $"{RefreshTokenPrefix}{token}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, userId.ToString(), options);
    }

    public async Task<Guid?> GetCachedRefreshTokenUserIdAsync(string token)
    {
        var key = $"{RefreshTokenPrefix}{token}";
        var val = await _cache.GetStringAsync(key);
        if (Guid.TryParse(val, out var userId))
        {
            return userId;
        }
        return null;
    }

    public async Task InvalidateRefreshTokenAsync(string token)
    {
        var key = $"{RefreshTokenPrefix}{token}";
        await _cache.RemoveAsync(key);
    }
}
