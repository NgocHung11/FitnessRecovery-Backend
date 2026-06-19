using System;
using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Recommendation.Domain;

public class Recommendation : Entity
{
    private Recommendation() { } // EF Core constructor

    public Recommendation(
        Guid userId,
        Guid recoveryAnalysisId,
        RecommendationType recommendationType,
        string message)
    {
        if (recoveryAnalysisId == Guid.Empty)
            throw new ArgumentException("Recovery analysis ID cannot be empty.", nameof(recoveryAnalysisId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));

        Id = Guid.NewGuid();
        UserId = userId;
        RecoveryAnalysisId = recoveryAnalysisId;
        RecommendationType = recommendationType;
        Message = message;
    }

    public Guid UserId { get; private set; }
    public Guid RecoveryAnalysisId { get; private set; }
    public RecommendationType RecommendationType { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public void Update(RecommendationType recommendationType, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));

        RecommendationType = recommendationType;
        Message = message;
        UpdateTimestamp();
    }

    public static Recommendation CreateFromScore(Guid userId, Guid recoveryAnalysisId, int recoveryScore)
    {
        var type = recoveryScore switch
        {
            >= 90 => RecommendationType.HighIntensityWorkout,
            >= 70 => RecommendationType.ModerateWorkout,
            >= 50 => RecommendationType.LightActivity,
            _ => RecommendationType.Rest
        };

        var message = type switch
        {
            RecommendationType.HighIntensityWorkout => "Your body is fully recovered. You are ready for a high intensity workout today!",
            RecommendationType.ModerateWorkout => "Good recovery level. A moderate workout is recommended today.",
            RecommendationType.LightActivity => "Moderate recovery. Keep it light today with walking, cycling, or active recovery.",
            RecommendationType.Rest => "Your recovery is low. We recommend taking a rest day or performing very light mobility work to recover.",
            _ => "Rest day is recommended."
        };

        return new Recommendation(userId, recoveryAnalysisId, type, message);
    }
}
