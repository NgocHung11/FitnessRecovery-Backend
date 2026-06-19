using System;
using System.Collections.Generic;

namespace FitnessRecovery.Features.Dashboard.DTOs;

public record AnalyticsDto(
    List<RecoveryTrendPointDto> WeeklyRecovery,
    List<RecoveryTrendPointDto> MonthlyRecovery,
    List<WorkoutTrendPointDto> WorkoutTrend,
    List<SleepTrendPointDto> SleepTrend);

public record RecoveryTrendPointDto(
    DateOnly Date,
    int? Score);

public record WorkoutTrendPointDto(
    DateOnly Date,
    int WorkoutCount,
    int TotalDurationMinutes,
    int TotalCaloriesBurned);

public record SleepTrendPointDto(
    DateOnly Date,
    double SleepHours,
    string? SleepQuality);
