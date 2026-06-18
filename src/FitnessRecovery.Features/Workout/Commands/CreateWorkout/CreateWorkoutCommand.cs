using FitnessRecovery.Features.Workout.Domain;

namespace FitnessRecovery.Features.Workout.Commands.CreateWorkout;

public record CreateWorkoutCommand(
    Guid UserId,
    WorkoutType WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    WorkoutIntensity Intensity,
    string? Notes,
    DateTime WorkoutDate);
