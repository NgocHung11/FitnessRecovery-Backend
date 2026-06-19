using System;
using System.Collections.Generic;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Recovery.Domain;

public class RecoveryAnalysis : Entity
{
    private RecoveryAnalysis() { } // EF Core constructor

    public RecoveryAnalysis(
        Guid userId,
        DateOnly analysisDate,
        RecoveryScore recoveryScore,
        RecoveryStatus recoveryStatus,
        int sleepScore,
        int heartRateScore,
        int workoutLoadScore,
        int activityScore)
    {
        if (sleepScore < 0 || sleepScore > 100)
            throw new ArgumentException("Sleep score must be between 0 and 100.", nameof(sleepScore));
        if (heartRateScore < 0 || heartRateScore > 100)
            throw new ArgumentException("Heart rate score must be between 0 and 100.", nameof(heartRateScore));
        if (workoutLoadScore < 0 || workoutLoadScore > 100)
            throw new ArgumentException("Workout load score must be between 0 and 100.", nameof(workoutLoadScore));
        if (activityScore < 0 || activityScore > 100)
            throw new ArgumentException("Activity score must be between 0 and 100.", nameof(activityScore));
        if (analysisDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Analysis date cannot be in the future.", nameof(analysisDate));

        Id = Guid.NewGuid();
        UserId = userId;
        AnalysisDate = analysisDate;
        RecoveryScore = recoveryScore;
        RecoveryStatus = recoveryStatus;
        SleepScore = sleepScore;
        HeartRateScore = heartRateScore;
        WorkoutLoadScore = workoutLoadScore;
        ActivityScore = activityScore;
        GeneratedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public DateOnly AnalysisDate { get; private set; }
    public RecoveryScore RecoveryScore { get; private set; }
    public RecoveryStatus RecoveryStatus { get; private set; }
    public int SleepScore { get; private set; }
    public int HeartRateScore { get; private set; }
    public int WorkoutLoadScore { get; private set; }
    public int ActivityScore { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    public void Update(
        RecoveryScore recoveryScore,
        RecoveryStatus recoveryStatus,
        int sleepScore,
        int heartRateScore,
        int workoutLoadScore,
        int activityScore)
    {
        if (sleepScore < 0 || sleepScore > 100)
            throw new ArgumentException("Sleep score must be between 0 and 100.", nameof(sleepScore));
        if (heartRateScore < 0 || heartRateScore > 100)
            throw new ArgumentException("Heart rate score must be between 0 and 100.", nameof(heartRateScore));
        if (workoutLoadScore < 0 || workoutLoadScore > 100)
            throw new ArgumentException("Workout load score must be between 0 and 100.", nameof(workoutLoadScore));
        if (activityScore < 0 || activityScore > 100)
            throw new ArgumentException("Activity score must be between 0 and 100.", nameof(activityScore));

        RecoveryScore = recoveryScore;
        RecoveryStatus = recoveryStatus;
        SleepScore = sleepScore;
        HeartRateScore = heartRateScore;
        WorkoutLoadScore = workoutLoadScore;
        ActivityScore = activityScore;
        GeneratedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public static RecoveryAnalysis Calculate(
        Guid userId,
        DateOnly analysisDate,
        HealthRecord todayHealth,
        HealthRecord? yesterdayHealth,
        List<WorkoutSession> yesterdayWorkouts)
    {
        // 1. Calculate Sleep Score (40%)
        double sleepHours = todayHealth.SleepHours.Value;
        double sleepHoursScore = (sleepHours / 8.0) * 100.0;
        if (sleepHoursScore > 100.0) sleepHoursScore = 100.0;

        double sleepQualityScore = todayHealth.SleepQuality switch
        {
            SleepQuality.Poor => 40.0,
            SleepQuality.Average => 60.0,
            SleepQuality.Good => 80.0,
            SleepQuality.Excellent => 100.0,
            _ => 60.0
        };

        double finalSleepScore = (sleepHoursScore * 0.7) + (sleepQualityScore * 0.3);

        // 2. Calculate Heart Rate Score (30%)
        int rhr = todayHealth.RestingHeartRate.Value;
        double finalHeartRateScore = 100.0 - (rhr - 50.0) * 2.0;
        if (finalHeartRateScore > 100.0) finalHeartRateScore = 100.0;
        if (finalHeartRateScore < 0.0) finalHeartRateScore = 0.0;

        // 3. Calculate Workout Load Score (20%)
        double totalLoad = 0.0;
        foreach (var workout in yesterdayWorkouts)
        {
            double multiplier = workout.Intensity switch
            {
                WorkoutIntensity.Low => 1.0,
                WorkoutIntensity.Moderate => 1.5,
                WorkoutIntensity.High => 2.0,
                _ => 1.0
            };
            totalLoad += workout.DurationMinutes * multiplier;
        }
        double finalWorkoutLoadScore = 100.0 - totalLoad;
        if (finalWorkoutLoadScore < 0.0) finalWorkoutLoadScore = 0.0;

        // 4. Calculate Activity Score (10%)
        int steps = yesterdayHealth?.Steps.Value ?? 10000;
        double finalActivityScore = (steps / 10000.0) * 100.0;
        if (finalActivityScore > 100.0) finalActivityScore = 100.0;

        // Final Recovery Score
        double rawScore = (finalSleepScore * 0.4) 
                        + (finalHeartRateScore * 0.3) 
                        + (finalWorkoutLoadScore * 0.2) 
                        + (finalActivityScore * 0.1);

        int scoreValue = (int)Math.Round(rawScore);

        var status = scoreValue switch
        {
            >= 90 => RecoveryStatus.Excellent,
            >= 70 => RecoveryStatus.Good,
            >= 50 => RecoveryStatus.Moderate,
            _ => RecoveryStatus.Poor
        };

        return new RecoveryAnalysis(
            userId,
            analysisDate,
            new RecoveryScore(scoreValue),
            status,
            (int)Math.Round(finalSleepScore),
            (int)Math.Round(finalHeartRateScore),
            (int)Math.Round(finalWorkoutLoadScore),
            (int)Math.Round(finalActivityScore));
    }
}
