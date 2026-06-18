using FitnessRecovery.Features.Workout.Domain;

namespace FitnessRecovery.Features.Workout.Commands.UpdateWorkout;

public record UpdateWorkoutCommand(
    Guid Id,
    Guid UserId,
    WorkoutType WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    WorkoutIntensity Intensity,
    string? Notes,
    DateTime WorkoutDate);
