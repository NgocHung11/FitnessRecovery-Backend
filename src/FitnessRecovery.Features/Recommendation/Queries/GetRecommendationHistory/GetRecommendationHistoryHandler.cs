using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recommendation.Queries.GetRecommendationHistory;

public class GetRecommendationHistoryHandler
{
    private readonly IRecommendationRepository _recommendationRepository;

    public GetRecommendationHistoryHandler(IRecommendationRepository recommendationRepository)
    {
        _recommendationRepository = recommendationRepository;
    }

    public async Task<Result<PagedList<RecommendationDto>>> HandleAsync(GetRecommendationHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result.Failure<PagedList<RecommendationDto>>(new Error("Recommendation.History", "Page index must be greater than zero."));
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            return Result.Failure<PagedList<RecommendationDto>>(new Error("Recommendation.History", "Page size must be between 1 and 100."));
        }

        var pagedRecommendations = await _recommendationRepository.GetPagedByUserIdAsync(query.UserId, query.Page, query.PageSize);

        var dtos = pagedRecommendations.Items.Select(rec => new RecommendationDto(
            rec.Id,
            rec.UserId,
            rec.RecoveryAnalysisId,
            rec.RecommendationType.ToString(),
            rec.Message,
            rec.CreatedAt)).ToList();

        var result = new PagedList<RecommendationDto>(dtos, pagedRecommendations.Page, pagedRecommendations.PageSize, pagedRecommendations.TotalItems);

        return Result.Success(result);
    }
}
