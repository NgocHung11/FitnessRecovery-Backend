using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecord;

public class GetHealthRecordHandler
{
    private readonly IHealthRecordRepository _healthRecordRepository;

    public GetHealthRecordHandler(IHealthRecordRepository healthRecordRepository)
    {
        _healthRecordRepository = healthRecordRepository;
    }

    public async Task<Result<HealthRecordDto>> HandleAsync(GetHealthRecordQuery query, CancellationToken cancellationToken = default)
    {
        var record = await _healthRecordRepository.GetByDateAsync(query.UserId, query.RecordDate);
        if (record is null)
        {
            return Result.Failure<HealthRecordDto>(new Error("Error.NotFound", $"Health record for date {query.RecordDate} was not found."));
        }

        var dto = new HealthRecordDto(
            record.Id,
            record.UserId,
            record.RecordDate,
            record.SleepHours.Value,
            record.SleepQuality.ToString(),
            record.RestingHeartRate.Value,
            record.AverageHeartRate.Value,
            record.Steps.Value,
            record.Weight,
            record.CaloriesBurned,
            record.CreatedAt,
            record.UpdatedAt);

        return Result.Success(dto);
    }
}
