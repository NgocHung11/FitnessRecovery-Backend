using System;

namespace FitnessRecovery.Features.Recommendation.DTOs;

public record RecommendationDto(
    Guid Id,
    Guid UserId,
    Guid RecoveryAnalysisId,
    string RecommendationType,
    string Message,
    DateTime CreatedAt);
