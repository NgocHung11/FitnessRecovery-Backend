using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Dashboard.DTOs;
using Microsoft.Extensions.Caching.Distributed;

namespace FitnessRecovery.Infrastructure.Caching;

public class DashboardCacheService : IDashboardCacheService
{
    private readonly IDistributedCache _cache;
    private const string DashboardPrefix = "dashboard:daily:";
    private const string WeeklyReportsPrefix = "dashboard:weekly:";

    public DashboardCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<DailyDashboardDto?> GetDailyDashboardAsync(Guid userId)
    {
        var key = $"{DashboardPrefix}{userId}";
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DailyDashboardDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetDailyDashboardAsync(Guid userId, DailyDashboardDto dashboard, TimeSpan expiration)
    {
        var key = $"{DashboardPrefix}{userId}";
        var json = JsonSerializer.Serialize(dashboard);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, json, options);
    }

    public async Task InvalidateDailyDashboardAsync(Guid userId)
    {
        var key = $"{DashboardPrefix}{userId}";
        await _cache.RemoveAsync(key);
    }

    public async Task<List<WeeklyReportDto>?> GetWeeklyReportsAsync(Guid userId)
    {
        var key = $"{WeeklyReportsPrefix}{userId}";
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<WeeklyReportDto>>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetWeeklyReportsAsync(Guid userId, List<WeeklyReportDto> reports, TimeSpan expiration)
    {
        var key = $"{WeeklyReportsPrefix}{userId}";
        var json = JsonSerializer.Serialize(reports);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, json, options);
    }

    public async Task InvalidateWeeklyReportsAsync(Guid userId)
    {
        var key = $"{WeeklyReportsPrefix}{userId}";
        await _cache.RemoveAsync(key);
    }
}
