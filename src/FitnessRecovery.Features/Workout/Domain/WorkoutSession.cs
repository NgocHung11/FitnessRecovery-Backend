using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Workout.Domain;

public class WorkoutSession : Entity
{
    private WorkoutSession() { } // EF Core constructor

    public WorkoutSession(
        Guid userId,
        WorkoutType workoutType,
        int durationMinutes,
        int caloriesBurned,
        WorkoutIntensity intensity,
        string? notes,
        DateTime workoutDate)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than zero.", nameof(durationMinutes));
        if (caloriesBurned < 0)
            throw new ArgumentException("Calories burned cannot be negative.", nameof(caloriesBurned));
        if (workoutDate > DateTime.UtcNow)
            throw new ArgumentException("Workout date cannot be in the future.", nameof(workoutDate));

        Id = Guid.NewGuid();
        UserId = userId;
        WorkoutType = workoutType;
        DurationMinutes = durationMinutes;
        CaloriesBurned = caloriesBurned;
        Intensity = intensity;
        Notes = notes;
        WorkoutDate = workoutDate;
    }

    public Guid UserId { get; private set; }
    public WorkoutType WorkoutType { get; private set; }
    public int DurationMinutes { get; private set; }
    public int CaloriesBurned { get; private set; }
    public WorkoutIntensity Intensity { get; private set; }
    public string? Notes { get; private set; }
    public DateTime WorkoutDate { get; private set; }

    public void Update(
        WorkoutType workoutType,
        int durationMinutes,
        int caloriesBurned,
        WorkoutIntensity intensity,
        string? notes,
        DateTime workoutDate)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than zero.", nameof(durationMinutes));
        if (caloriesBurned < 0)
            throw new ArgumentException("Calories burned cannot be negative.", nameof(caloriesBurned));
        if (workoutDate > DateTime.UtcNow)
            throw new ArgumentException("Workout date cannot be in the future.", nameof(workoutDate));

        WorkoutType = workoutType;
        DurationMinutes = durationMinutes;
        CaloriesBurned = caloriesBurned;
        Intensity = intensity;
        Notes = notes;
        WorkoutDate = workoutDate;
        UpdateTimestamp();
    }
}
// 
