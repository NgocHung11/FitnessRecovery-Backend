using System;
using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Health.Domain;

public class HealthRecord : Entity
{
    private HealthRecord() { } // EF Core constructor

    public HealthRecord(
        Guid userId,
        DateOnly recordDate,
        SleepHours sleepHours,
        SleepQuality sleepQuality,
        HeartRate restingHeartRate,
        HeartRate averageHeartRate,
        Steps steps,
        double weight,
        int caloriesBurned)
    {
        if (weight <= 0.0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        if (caloriesBurned < 0)
            throw new ArgumentException("Calories burned cannot be negative.", nameof(caloriesBurned));
        if (recordDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Record date cannot be in the future.", nameof(recordDate));

        Id = Guid.NewGuid();
        UserId = userId;
        RecordDate = recordDate;
        SleepHours = sleepHours;
        SleepQuality = sleepQuality;
        RestingHeartRate = restingHeartRate;
        AverageHeartRate = averageHeartRate;
        Steps = steps;
        Weight = weight;
        CaloriesBurned = caloriesBurned;
    }

    public Guid UserId { get; private set; }
    public DateOnly RecordDate { get; private set; }
    public SleepHours SleepHours { get; private set; }
    public SleepQuality SleepQuality { get; private set; }
    public HeartRate RestingHeartRate { get; private set; }
    public HeartRate AverageHeartRate { get; private set; }
    public Steps Steps { get; private set; }
    public double Weight { get; private set; }
    public int CaloriesBurned { get; private set; }

    public void Update(
        SleepHours sleepHours,
        SleepQuality sleepQuality,
        HeartRate restingHeartRate,
        HeartRate averageHeartRate,
        Steps steps,
        double weight,
        int caloriesBurned)
    {
        if (weight <= 0.0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        if (caloriesBurned < 0)
            throw new ArgumentException("Calories burned cannot be negative.", nameof(caloriesBurned));

        SleepHours = sleepHours;
        SleepQuality = sleepQuality;
        RestingHeartRate = restingHeartRate;
        AverageHeartRate = averageHeartRate;
        Steps = steps;
        Weight = weight;
        CaloriesBurned = caloriesBurned;
        UpdateTimestamp();
    }
}
