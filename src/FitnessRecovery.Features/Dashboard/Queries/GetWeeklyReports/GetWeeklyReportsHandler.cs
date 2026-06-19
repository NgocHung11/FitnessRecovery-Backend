using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Dashboard.Queries.GetWeeklyReports;

public class GetWeeklyReportsHandler
{
    private readonly IWeeklyReportMongoRepository _weeklyReportRepository;
    private readonly IDashboardCacheService _dashboardCacheService;

    public GetWeeklyReportsHandler(
        IWeeklyReportMongoRepository weeklyReportRepository,
        IDashboardCacheService dashboardCacheService)
    {
        _weeklyReportRepository = weeklyReportRepository;
        _dashboardCacheService = dashboardCacheService;
    }

    public async Task<Result<List<WeeklyReportDto>>> HandleAsync(GetWeeklyReportsQuery query, CancellationToken cancellationToken = default)
    {
        // Try get from cache
        var cachedReports = await _dashboardCacheService.GetWeeklyReportsAsync(query.UserId);
        if (cachedReports is not null)
        {
            return Result.Success(cachedReports);
        }

        var reports = await _weeklyReportRepository.GetByUserIdAsync(query.UserId);

        var dtos = reports.Select(r => new WeeklyReportDto(
            r.Id,
            r.UserId,
            r.StartDate,
            r.EndDate,
            r.AverageRecoveryScore,
            r.AverageSleepHours,
            r.TotalWorkoutDuration,
            r.TotalCaloriesBurned,
            r.TotalSteps,
            r.GeneratedAt
        )).ToList();

        // Save to cache (TTL: 24 hours since weekly reports change infrequently)
        await _dashboardCacheService.SetWeeklyReportsAsync(query.UserId, dtos, TimeSpan.FromHours(24));

        return Result.Success(dtos);
    }
}
