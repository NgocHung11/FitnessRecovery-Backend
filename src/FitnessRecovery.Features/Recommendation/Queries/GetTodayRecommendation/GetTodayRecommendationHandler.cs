using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.DTOs;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recommendation.Queries.GetTodayRecommendation;

public class GetTodayRecommendationHandler
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly GetTodayRecoveryHandler _getTodayRecoveryHandler;

    public GetTodayRecommendationHandler(
        IRecommendationRepository recommendationRepository,
        GetTodayRecoveryHandler getTodayRecoveryHandler)
    {
        _recommendationRepository = recommendationRepository;
        _getTodayRecoveryHandler = getTodayRecoveryHandler;
    }

    public async Task<Result<RecommendationDto>> HandleAsync(GetTodayRecommendationQuery query, CancellationToken cancellationToken = default)
    {
        // 1. Trigger recovery analysis on-the-fly to compute recovery score and upsert recommendation
        var recoveryResult = await _getTodayRecoveryHandler.HandleAsync(new GetTodayRecoveryQuery(query.UserId), cancellationToken);
        if (recoveryResult.IsFailure)
        {
            return Result.Failure<RecommendationDto>(recoveryResult.Error);
        }

        var recoveryAnalysis = recoveryResult.Value;

        // 2. Fetch the corresponding recommendation
        var recommendation = await _recommendationRepository.GetByAnalysisIdAsync(recoveryAnalysis.Id);
        if (recommendation is null)
        {
            return Result.Failure<RecommendationDto>(new Error("Recommendation.NotFound", "Recommendation could not be found for today's recovery analysis."));
        }

        // 3. Map to DTO
        var dto = new RecommendationDto(
            recommendation.Id,
            recommendation.UserId,
            recommendation.RecoveryAnalysisId,
            recommendation.RecommendationType.ToString(),
            recommendation.Message,
            recommendation.CreatedAt);

        return Result.Success(dto);
    }
}
