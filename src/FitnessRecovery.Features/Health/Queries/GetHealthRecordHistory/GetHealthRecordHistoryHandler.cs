using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecordHistory;

public class GetHealthRecordHistoryHandler
{
    private readonly IHealthRecordRepository _healthRecordRepository;

    public GetHealthRecordHistoryHandler(IHealthRecordRepository healthRecordRepository)
    {
        _healthRecordRepository = healthRecordRepository;
    }

    public async Task<Result<PagedList<HealthRecordDto>>> HandleAsync(GetHealthRecordHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result.Failure<PagedList<HealthRecordDto>>(new Error("HealthRecord.History", "Page index must be greater than zero."));
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            return Result.Failure<PagedList<HealthRecordDto>>(new Error("HealthRecord.History", "Page size must be between 1 and 100."));
        }

        var pagedRecords = await _healthRecordRepository.GetPagedByUserIdAsync(query.UserId, query.Page, query.PageSize);

        var dtos = pagedRecords.Items.Select(record => new HealthRecordDto(
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
            record.UpdatedAt)).ToList();

        var result = new PagedList<HealthRecordDto>(dtos, pagedRecords.Page, pagedRecords.PageSize, pagedRecords.TotalItems);

        return Result.Success(result);
    }
}
