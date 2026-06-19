using System;
using System.Text.Json;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.DTOs;
using Microsoft.Extensions.Caching.Distributed;

namespace FitnessRecovery.Infrastructure.Caching;

public class RecoveryCacheService : IRecoveryCacheService
{
    private readonly IDistributedCache _cache;
    private const string RecoveryPrefix = "recovery:today:";

    public RecoveryCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<RecoveryAnalysisDto?> GetTodayRecoveryAsync(Guid userId)
    {
        var key = $"{RecoveryPrefix}{userId}";
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RecoveryAnalysisDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTodayRecoveryAsync(Guid userId, RecoveryAnalysisDto analysis, TimeSpan expiration)
    {
        var key = $"{RecoveryPrefix}{userId}";
        var json = JsonSerializer.Serialize(analysis);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, json, options);
    }

    public async Task InvalidateTodayRecoveryAsync(Guid userId)
    {
        var key = $"{RecoveryPrefix}{userId}";
        await _cache.RemoveAsync(key);
    }
}
