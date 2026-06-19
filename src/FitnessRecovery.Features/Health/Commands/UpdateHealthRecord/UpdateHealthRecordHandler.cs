using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;

public class UpdateHealthRecordHandler
{
    private readonly IHealthRecordRepository _healthRecordRepository;
    private readonly IHealthRecordMongoRepository _healthRecordMongoRepository;
    private readonly IRecoveryCacheService _recoveryCacheService;
    private readonly IDashboardCacheService _dashboardCacheService;

    public UpdateHealthRecordHandler(
        IHealthRecordRepository healthRecordRepository,
        IHealthRecordMongoRepository healthRecordMongoRepository,
        IRecoveryCacheService recoveryCacheService,
        IDashboardCacheService dashboardCacheService)
    {
        _healthRecordRepository = healthRecordRepository;
        _healthRecordMongoRepository = healthRecordMongoRepository;
        _recoveryCacheService = recoveryCacheService;
        _dashboardCacheService = dashboardCacheService;
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
            await _healthRecordMongoRepository.UpsertAsync(record);

            // Invalidate caches
            await _recoveryCacheService.InvalidateTodayRecoveryAsync(command.UserId);
            await _dashboardCacheService.InvalidateDailyDashboardAsync(command.UserId);
            await _dashboardCacheService.InvalidateWeeklyReportsAsync(command.UserId);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("HealthRecord.Validation", ex.Message));
        }
    }
}
