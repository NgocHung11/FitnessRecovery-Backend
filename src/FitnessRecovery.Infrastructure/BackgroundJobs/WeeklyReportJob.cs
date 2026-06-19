using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Dashboard.Domain;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using Microsoft.Extensions.Logging;

namespace FitnessRecovery.Infrastructure.BackgroundJobs;

public class WeeklyReportJob
{
    private readonly IUserRepository _userRepository;
    private readonly IHealthRecordRepository _healthRecordRepository;
    private readonly IRecoveryRepository _recoveryRepository;
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IWeeklyReportMongoRepository _weeklyReportMongoRepository;
    private readonly IDashboardCacheService _dashboardCacheService;
    private readonly ILogger<WeeklyReportJob> _logger;

    public WeeklyReportJob(
        IUserRepository userRepository,
        IHealthRecordRepository healthRecordRepository,
        IRecoveryRepository recoveryRepository,
        IWorkoutRepository workoutRepository,
        IWeeklyReportMongoRepository weeklyReportMongoRepository,
        IDashboardCacheService dashboardCacheService,
        ILogger<WeeklyReportJob> logger)
    {
        _userRepository = userRepository;
        _healthRecordRepository = healthRecordRepository;
        _recoveryRepository = recoveryRepository;
        _workoutRepository = workoutRepository;
        _weeklyReportMongoRepository = weeklyReportMongoRepository;
        _dashboardCacheService = dashboardCacheService;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Find Monday of the completed week. Since this job runs once a week (e.g. Sunday midnight/Monday morning),
        // today.AddDays(-1) refers to a day in the week that just finished (Sunday).
        // GetStartOfWeek on Sunday will return Monday of that week.
        var startOfWeek = GetStartOfWeek(today.AddDays(-1));
        var endOfWeek = startOfWeek.AddDays(6);

        _logger.LogInformation("Starting Weekly Report Job for completed week: {Start} to {End}", startOfWeek, endOfWeek);

        var users = await _userRepository.GetAllUsersAsync();

        foreach (var user in users)
        {
            try
            {
                var weekHealth = await _healthRecordRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek);
                var weekAnalyses = await _recoveryRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek);
                var weekWorkouts = await _workoutRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek);

                if (weekHealth.Any() || weekAnalyses.Any() || weekWorkouts.Any())
                {
                    double avgRecovery = weekAnalyses.Any() ? weekAnalyses.Average(a => a.RecoveryScore.Value) : 0;
                    double avgSleep = weekHealth.Any() ? weekHealth.Average(h => h.SleepHours.Value) : 0;
                    int totalDuration = weekWorkouts.Sum(w => w.DurationMinutes);
                    int totalCalories = weekWorkouts.Sum(w => w.CaloriesBurned);
                    int totalSteps = weekHealth.Sum(h => h.Steps.Value);

                    var report = new WeeklyReport(
                        user.Id,
                        startOfWeek,
                        endOfWeek,
                        avgRecovery,
                        avgSleep,
                        totalDuration,
                        totalCalories,
                        totalSteps
                    );

                    await _weeklyReportMongoRepository.UpsertAsync(report);
                    await _dashboardCacheService.InvalidateWeeklyReportsAsync(user.Id);

                    _logger.LogInformation("Successfully generated and saved weekly report for user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogDebug("No data found for user {UserId} in week {Start} to {End}, skipping report compilation.", user.Id, startOfWeek, endOfWeek);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred compiling weekly report for user {UserId}", user.Id);
            }
        }

        _logger.LogInformation("Completed Weekly Report Job.");
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff);
    }
}
