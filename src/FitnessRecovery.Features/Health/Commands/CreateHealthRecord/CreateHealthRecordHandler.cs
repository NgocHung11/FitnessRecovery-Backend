using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Commands.CreateHealthRecord;

public class CreateHealthRecordHandler
{
    private readonly IHealthRecordRepository _healthRecordRepository;

    public CreateHealthRecordHandler(IHealthRecordRepository healthRecordRepository)
    {
        _healthRecordRepository = healthRecordRepository;
    }

    public async Task<Result<Guid>> HandleAsync(CreateHealthRecordCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRecord = await _healthRecordRepository.GetByDateAsync(command.UserId, command.RecordDate);
            if (existingRecord is not null)
            {
                return Result.Failure<Guid>(new Error("HealthRecord.AlreadyExists", $"A health record already exists for date {command.RecordDate}."));
            }

            if (!Enum.TryParse<SleepQuality>(command.SleepQuality, ignoreCase: true, out var sleepQuality))
            {
                return Result.Failure<Guid>(new Error("HealthRecord.Validation", "Invalid sleep quality."));
            }

            var record = new HealthRecord(
                command.UserId,
                command.RecordDate,
                new SleepHours(command.SleepHours),
                sleepQuality,
                new HeartRate(command.RestingHeartRate),
                new HeartRate(command.AverageHeartRate),
                new Steps(command.Steps),
                command.Weight,
                command.CaloriesBurned);

            await _healthRecordRepository.AddAsync(record);

            return Result.Success(record.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("HealthRecord.Validation", ex.Message));
        }
    }
}
