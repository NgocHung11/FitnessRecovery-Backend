using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.DTOs;

namespace FitnessRecovery.Features.Dashboard.Contracts;

public interface IDashboardCacheService
{
    Task<DailyDashboardDto?> GetDailyDashboardAsync(Guid userId);
    Task SetDailyDashboardAsync(Guid userId, DailyDashboardDto dashboard, TimeSpan expiration);
    Task InvalidateDailyDashboardAsync(Guid userId);

    Task<List<WeeklyReportDto>?> GetWeeklyReportsAsync(Guid userId);
    Task SetWeeklyReportsAsync(Guid userId, List<WeeklyReportDto> reports, TimeSpan expiration);
    Task InvalidateWeeklyReportsAsync(Guid userId);
}
