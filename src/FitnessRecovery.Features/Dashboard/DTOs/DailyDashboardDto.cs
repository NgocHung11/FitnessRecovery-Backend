using System;
using System.Collections.Generic;

namespace FitnessRecovery.Features.Dashboard.DTOs;

public record DailyDashboardDto(
    int? RecoveryScore,
    string? RecoveryStatus,
    double? SleepHours,
    string? SleepQuality,
    int? Steps,
    int StepGoal,
    List<DashboardWorkoutDto> Workouts);

public record DashboardWorkoutDto(
    Guid Id,
    string WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    string Intensity);
