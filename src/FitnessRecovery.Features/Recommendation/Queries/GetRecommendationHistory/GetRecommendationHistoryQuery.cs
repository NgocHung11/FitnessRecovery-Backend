using System;

namespace FitnessRecovery.Features.Recommendation.Queries.GetRecommendationHistory;

public record GetRecommendationHistoryQuery(Guid UserId, int Page = 1, int PageSize = 10);
