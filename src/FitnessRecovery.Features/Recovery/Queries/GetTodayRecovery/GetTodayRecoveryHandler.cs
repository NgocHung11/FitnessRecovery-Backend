using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;

public class GetTodayRecoveryHandler
{
    private readonly IRecoveryRepository _recoveryRepository;
    private readonly IHealthRecordRepository _healthRecordRepository;
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IRecommendationRepository _recommendationRepository;

    public GetTodayRecoveryHandler(
        IRecoveryRepository recoveryRepository,
        IHealthRecordRepository healthRecordRepository,
        IWorkoutRepository workoutRepository,
        IRecommendationRepository recommendationRepository)
    {
        _recoveryRepository = recoveryRepository;
        _healthRecordRepository = healthRecordRepository;
        _workoutRepository = workoutRepository;
        _recommendationRepository = recommendationRepository;
    }

    public async Task<Result<RecoveryAnalysisDto>> HandleAsync(GetTodayRecoveryQuery query, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        // 1. Fetch today's health record
        var todayHealth = await _healthRecordRepository.GetByDateAsync(query.UserId, today);
        if (todayHealth is null)
        {
            return Result.Failure<RecoveryAnalysisDto>(new Error("Recovery.HealthRecordMissing", "Today's health record has not been recorded yet. Please record your health metrics first."));
        }

        // 2. Fetch yesterday's health record and workouts
        var yesterdayHealth = await _healthRecordRepository.GetByDateAsync(query.UserId, yesterday);
        var yesterdayWorkouts = await _workoutRepository.GetWorkoutsForDateAsync(query.UserId, yesterday);

        // 3. Perform recovery calculation
        var newAnalysis = RecoveryAnalysis.Calculate(
            query.UserId,
            today,
            todayHealth,
            yesterdayHealth,
            yesterdayWorkouts);

        // 4. Check if today's analysis already exists (upsert)
        var existingAnalysis = await _recoveryRepository.GetByDateAsync(query.UserId, today);
        if (existingAnalysis is not null)
        {
            existingAnalysis.Update(
                newAnalysis.RecoveryScore,
                newAnalysis.RecoveryStatus,
                newAnalysis.SleepScore,
                newAnalysis.HeartRateScore,
                newAnalysis.WorkoutLoadScore,
                newAnalysis.ActivityScore);

            await _recoveryRepository.UpdateAsync(existingAnalysis);
            newAnalysis = existingAnalysis; // use updated entity for DTO
        }
        else
        {
            await _recoveryRepository.AddAsync(newAnalysis);
        }

        // 4.5. Upsert recommendation
        var recommendation = FitnessRecovery.Features.Recommendation.Domain.Recommendation.CreateFromScore(query.UserId, newAnalysis.Id, newAnalysis.RecoveryScore.Value);
        var existingRecommendation = await _recommendationRepository.GetByAnalysisIdAsync(newAnalysis.Id);
        if (existingRecommendation is not null)
        {
            existingRecommendation.Update(recommendation.RecommendationType, recommendation.Message);
            await _recommendationRepository.UpdateAsync(existingRecommendation);
        }
        else
        {
            await _recommendationRepository.AddAsync(recommendation);
        }

        // 5. Map to DTO
        var dto = new RecoveryAnalysisDto(
            newAnalysis.Id,
            newAnalysis.UserId,
            newAnalysis.AnalysisDate,
            newAnalysis.RecoveryScore.Value,
            newAnalysis.RecoveryStatus.ToString(),
            newAnalysis.SleepScore,
            newAnalysis.HeartRateScore,
            newAnalysis.WorkoutLoadScore,
            newAnalysis.ActivityScore,
            newAnalysis.GeneratedAt);

        return Result.Success(dto);
    }
}
