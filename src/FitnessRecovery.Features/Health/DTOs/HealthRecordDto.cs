using System;

namespace FitnessRecovery.Features.Health.DTOs;

public record HealthRecordDto(
    Guid Id,
    Guid UserId,
    DateOnly RecordDate,
    double SleepHours,
    string SleepQuality,
    int RestingHeartRate,
    int AverageHeartRate,
    int Steps,
    double Weight,
    int CaloriesBurned,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
