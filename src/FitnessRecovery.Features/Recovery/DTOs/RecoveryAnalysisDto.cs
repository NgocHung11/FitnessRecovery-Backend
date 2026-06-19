using System;

namespace FitnessRecovery.Features.Recovery.DTOs;

public record RecoveryAnalysisDto(
    Guid Id,
    Guid UserId,
    DateOnly AnalysisDate,
    int RecoveryScore,
    string RecoveryStatus,
    int SleepScore,
    int HeartRateScore,
    int WorkoutLoadScore,
    int ActivityScore,
    DateTime GeneratedAt);
