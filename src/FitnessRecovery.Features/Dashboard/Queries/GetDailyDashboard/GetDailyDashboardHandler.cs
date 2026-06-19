using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Dashboard.Queries.GetDailyDashboard;

public class GetDailyDashboardHandler
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IHealthRecordRepository _healthRecordRepository;
    private readonly GetTodayRecoveryHandler _getTodayRecoveryHandler;

    public GetDailyDashboardHandler(
        IWorkoutRepository workoutRepository,
        IHealthRecordRepository healthRecordRepository,
        GetTodayRecoveryHandler getTodayRecoveryHandler)
    {
        _workoutRepository = workoutRepository;
        _healthRecordRepository = healthRecordRepository;
        _getTodayRecoveryHandler = getTodayRecoveryHandler;
    }

    public async Task<Result<DailyDashboardDto>> HandleAsync(GetDailyDashboardQuery query, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Fetch workouts for today
        var workouts = await _workoutRepository.GetWorkoutsForDateAsync(query.UserId, today);
        var workoutDtos = workouts.Select(w => new DashboardWorkoutDto(
            w.Id,
            w.WorkoutType.ToString(),
            w.DurationMinutes,
            w.CaloriesBurned,
            w.Intensity.ToString()
        )).ToList();

        // 2. Fetch today's health record (if logged)
        var todayHealth = await _healthRecordRepository.GetByDateAsync(query.UserId, today);
        double? sleepHours = todayHealth?.SleepHours.Value;
        string? sleepQuality = todayHealth?.SleepQuality.ToString();
        int? steps = todayHealth?.Steps.Value;

        // 3. Retrieve or calculate recovery score (graceful degradation if health record is missing)
        int? recoveryScore = null;
        string? recoveryStatus = null;

        var recoveryResult = await _getTodayRecoveryHandler.HandleAsync(new GetTodayRecoveryQuery(query.UserId), cancellationToken);
        if (recoveryResult.IsSuccess)
        {
            recoveryScore = recoveryResult.Value.RecoveryScore;
            recoveryStatus = recoveryResult.Value.RecoveryStatus;
        }

        var dashboard = new DailyDashboardDto(
            recoveryScore,
            recoveryStatus,
            sleepHours,
            sleepQuality,
            steps,
            10000, // Default daily step goal
            workoutDtos
        );

        return Result.Success(dashboard);
    }
}
