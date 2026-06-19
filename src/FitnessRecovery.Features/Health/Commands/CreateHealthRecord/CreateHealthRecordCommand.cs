using System;

namespace FitnessRecovery.Features.Health.Commands.CreateHealthRecord;

public record CreateHealthRecordCommand(
    Guid UserId,
    DateOnly RecordDate,
    double SleepHours,
    string SleepQuality,
    int RestingHeartRate,
    int AverageHeartRate,
    int Steps,
    double Weight,
    int CaloriesBurned);
