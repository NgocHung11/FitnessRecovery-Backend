using FitnessRecovery.Features.Workout.Domain;

namespace FitnessRecovery.Features.Workout.DTOs;

public record WorkoutSessionDto(
    Guid Id,
    Guid UserId,
    string WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    string Intensity,
    string? Notes,
    DateTime WorkoutDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
