using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Dashboard.Queries.GetAnalytics;

public class GetAnalyticsHandler
{
    private readonly IRecoveryRepository _recoveryRepository;
    private readonly IHealthRecordRepository _healthRecordRepository;
    private readonly IWorkoutRepository _workoutRepository;

    public GetAnalyticsHandler(
        IRecoveryRepository recoveryRepository,
        IHealthRecordRepository healthRecordRepository,
        IWorkoutRepository workoutRepository)
    {
        _recoveryRepository = recoveryRepository;
        _healthRecordRepository = healthRecordRepository;
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<AnalyticsDto>> HandleAsync(GetAnalyticsQuery query, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weeklyStartDate = today.AddDays(-6);
        var monthlyStartDate = today.AddDays(-29);

        // Fetch records
        var recoveryAnalyses = await _recoveryRepository.GetByDateRangeAsync(query.UserId, monthlyStartDate, today);
        var healthRecords = await _healthRecordRepository.GetByDateRangeAsync(query.UserId, weeklyStartDate, today);
        var workouts = await _workoutRepository.GetByDateRangeAsync(query.UserId, weeklyStartDate, today);

        // 1. Weekly Recovery Trend (last 7 days)
        var weeklyRecoveryList = new List<RecoveryTrendPointDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var analysis = recoveryAnalyses.FirstOrDefault(r => r.AnalysisDate == date);
            weeklyRecoveryList.Add(new RecoveryTrendPointDto(date, analysis?.RecoveryScore.Value));
        }

        // 2. Monthly Recovery Trend (last 30 days)
        var monthlyRecoveryList = new List<RecoveryTrendPointDto>();
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var analysis = recoveryAnalyses.FirstOrDefault(r => r.AnalysisDate == date);
            monthlyRecoveryList.Add(new RecoveryTrendPointDto(date, analysis?.RecoveryScore.Value));
        }

        // 3. Workout Trend (last 7 days)
        var workoutTrendList = new List<WorkoutTrendPointDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            // WorkoutDate is UTC DateTime; convert to DateOnly for matching
            var dayWorkouts = workouts.Where(w => DateOnly.FromDateTime(w.WorkoutDate) == date).ToList();
            workoutTrendList.Add(new WorkoutTrendPointDto(
                date,
                dayWorkouts.Count,
                dayWorkouts.Sum(w => w.DurationMinutes),
                dayWorkouts.Sum(w => w.CaloriesBurned)
            ));
        }

        // 4. Sleep Trend (last 7 days)
        var sleepTrendList = new List<SleepTrendPointDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var record = healthRecords.FirstOrDefault(h => h.RecordDate == date);
            sleepTrendList.Add(new SleepTrendPointDto(
                date,
                record?.SleepHours.Value ?? 0,
                record?.SleepQuality.ToString()
            ));
        }

        var analytics = new AnalyticsDto(
            weeklyRecoveryList,
            monthlyRecoveryList,
            workoutTrendList,
            sleepTrendList
        );

        return Result.Success(analytics);
    }
}
