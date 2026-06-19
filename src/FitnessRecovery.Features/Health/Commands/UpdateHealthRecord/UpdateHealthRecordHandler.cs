using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;

public class UpdateHealthRecordHandler
{
    private readonly IHealthRecordRepository _healthRecordRepository;

    public UpdateHealthRecordHandler(IHealthRecordRepository healthRecordRepository)
    {
        _healthRecordRepository = healthRecordRepository;
    }

    public async Task<Result> HandleAsync(UpdateHealthRecordCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _healthRecordRepository.GetByDateAsync(command.UserId, command.RecordDate);
            if (record is null)
            {
                return Result.Failure(new Error("Error.NotFound", $"Health record for date {command.RecordDate} was not found."));
            }

            if (!Enum.TryParse<SleepQuality>(command.SleepQuality, ignoreCase: true, out var sleepQuality))
            {
                return Result.Failure(new Error("HealthRecord.Validation", "Invalid sleep quality."));
            }

            record.Update(
                new SleepHours(command.SleepHours),
                sleepQuality,
                new HeartRate(command.RestingHeartRate),
                new HeartRate(command.AverageHeartRate),
                new Steps(command.Steps),
                command.Weight,
                command.CaloriesBurned);

            await _healthRecordRepository.UpdateAsync(record);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("HealthRecord.Validation", ex.Message));
        }
    }
}
