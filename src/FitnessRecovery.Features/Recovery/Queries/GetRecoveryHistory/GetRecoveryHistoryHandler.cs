using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recovery.Queries.GetRecoveryHistory;

public class GetRecoveryHistoryHandler
{
    private readonly IRecoveryRepository _recoveryRepository;

    public GetRecoveryHistoryHandler(IRecoveryRepository recoveryRepository)
    {
        _recoveryRepository = recoveryRepository;
    }

    public async Task<Result<PagedList<RecoveryAnalysisDto>>> HandleAsync(GetRecoveryHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result.Failure<PagedList<RecoveryAnalysisDto>>(new Error("Recovery.History", "Page index must be greater than zero."));
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            return Result.Failure<PagedList<RecoveryAnalysisDto>>(new Error("Recovery.History", "Page size must be between 1 and 100."));
        }

        var pagedAnalyses = await _recoveryRepository.GetPagedByUserIdAsync(query.UserId, query.Page, query.PageSize);

        var dtos = pagedAnalyses.Items.Select(analysis => new RecoveryAnalysisDto(
            analysis.Id,
            analysis.UserId,
            analysis.AnalysisDate,
            analysis.RecoveryScore.Value,
            analysis.RecoveryStatus.ToString(),
            analysis.SleepScore,
            analysis.HeartRateScore,
            analysis.WorkoutLoadScore,
            analysis.ActivityScore,
            analysis.GeneratedAt)).ToList();

        var result = new PagedList<RecoveryAnalysisDto>(dtos, pagedAnalyses.Page, pagedAnalyses.PageSize, pagedAnalyses.TotalItems);

        return Result.Success(result);
    }
}
