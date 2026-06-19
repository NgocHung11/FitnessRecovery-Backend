using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.Domain;
using FitnessRecovery.Features.Recommendation.Queries.GetTodayRecommendation;
using FitnessRecovery.Features.Recommendation.Queries.GetRecommendationHistory;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class RecommendationTests
{
    private readonly IRecoveryRepository _recoveryRepository = Substitute.For<IRecoveryRepository>();
    private readonly IHealthRecordRepository _healthRecordRepository = Substitute.For<IHealthRecordRepository>();
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly IRecommendationRepository _recommendationRepository = Substitute.For<IRecommendationRepository>();
    private readonly IRecoveryAnalysisMongoRepository _recoveryAnalysisMongoRepository = Substitute.For<IRecoveryAnalysisMongoRepository>();
    private readonly IRecoveryCacheService _recoveryCacheService = Substitute.For<IRecoveryCacheService>();

    private readonly GetTodayRecoveryHandler _getTodayRecoveryHandler;
    private readonly GetTodayRecommendationHandler _todayRecommendationHandler;
    private readonly GetRecommendationHistoryHandler _recommendationHistoryHandler;

    public RecommendationTests()
    {
        _getTodayRecoveryHandler = new GetTodayRecoveryHandler(
            _recoveryRepository,
            _healthRecordRepository,
            _workoutRepository,
            _recommendationRepository,
            _recoveryAnalysisMongoRepository,
            _recoveryCacheService);

        _todayRecommendationHandler = new GetTodayRecommendationHandler(_recommendationRepository, _getTodayRecoveryHandler);
        _recommendationHistoryHandler = new GetRecommendationHistoryHandler(_recommendationRepository);
    }

    [Theory]
    [InlineData(95, RecommendationType.HighIntensityWorkout, "Your body is fully recovered. You are ready for a high intensity workout today!")]
    [InlineData(90, RecommendationType.HighIntensityWorkout, "Your body is fully recovered. You are ready for a high intensity workout today!")]
    [InlineData(85, RecommendationType.ModerateWorkout, "Good recovery level. A moderate workout is recommended today.")]
    [InlineData(70, RecommendationType.ModerateWorkout, "Good recovery level. A moderate workout is recommended today.")]
    [InlineData(65, RecommendationType.LightActivity, "Moderate recovery. Keep it light today with walking, cycling, or active recovery.")]
    [InlineData(50, RecommendationType.LightActivity, "Moderate recovery. Keep it light today with walking, cycling, or active recovery.")]
    [InlineData(45, RecommendationType.Rest, "Your recovery is low. We recommend taking a rest day or performing very light mobility work to recover.")]
    [InlineData(10, RecommendationType.Rest, "Your recovery is low. We recommend taking a rest day or performing very light mobility work to recover.")]
    public void CreateFromScore_ShouldMapCorrectly(int score, RecommendationType expectedType, string expectedMessage)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();

        // Act
        var recommendation = Recommendation.CreateFromScore(userId, analysisId, score);

        // Assert
        recommendation.RecommendationType.Should().Be(expectedType);
        recommendation.Message.Should().Be(expectedMessage);
        recommendation.UserId.Should().Be(userId);
        recommendation.RecoveryAnalysisId.Should().Be(analysisId);
    }

    [Fact]
    public void Recommendation_Constructor_ShouldThrowException_WhenAnalysisIdIsEmpty()
    {
        // Act
        Action act = () => new Recommendation(Guid.NewGuid(), Guid.Empty, RecommendationType.Rest, "Rest");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Recovery analysis ID cannot be empty.*");
    }

    [Fact]
    public void Recommendation_Constructor_ShouldThrowException_WhenMessageIsEmpty()
    {
        // Act
        Action act = () => new Recommendation(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.Rest, " ");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Message cannot be null or empty.*");
    }

    [Fact]
    public void Recommendation_Update_ShouldThrowException_WhenMessageIsEmpty()
    {
        // Arrange
        var recommendation = Recommendation.CreateFromScore(Guid.NewGuid(), Guid.NewGuid(), 80);

        // Act
        Action act = () => recommendation.Update(RecommendationType.Rest, "");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Message cannot be null or empty.*");
    }

    [Fact]
    public async Task GetTodayRecommendation_ShouldReturnFailure_WhenRecoveryAnalysisFails_DueToMissingHealthRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _healthRecordRepository.GetByDateAsync(userId, today).Returns((HealthRecord)null);

        var query = new GetTodayRecommendationQuery(userId);

        // Act
        var result = await _todayRecommendationHandler.HandleAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Recovery.HealthRecordMissing");
    }

    [Fact]
    public async Task GetTodayRecommendation_ShouldReturnSuccess_WhenRecoveryAnalysisSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayHealth = new HealthRecord(userId, today, 8.0, SleepQuality.Excellent, 50, 80, 5000, 70.0, 300);

        _healthRecordRepository.GetByDateAsync(userId, today).Returns(todayHealth);
        _workoutRepository.GetWorkoutsForDateAsync(userId, today.AddDays(-1)).Returns(new List<WorkoutSession>());
        
        // Mock existing analysis to be null (first generation)
        _recoveryRepository.GetByDateAsync(userId, today).Returns((RecoveryAnalysis)null);

        // Capture/mock the recommendation that will be added/retrieved
        Recommendation savedRecommendation = null;
        _recommendationRepository.AddAsync(Arg.Do<Recommendation>(r => savedRecommendation = r))
            .Returns(Task.CompletedTask);

        _recommendationRepository.GetByAnalysisIdAsync(Arg.Any<Guid>())
            .Returns(x => savedRecommendation);

        var query = new GetTodayRecommendationQuery(userId);

        // Act
        var result = await _todayRecommendationHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.Message.Should().NotBeNullOrEmpty();
        
        // Verify that the repository received calls
        await _recommendationRepository.Received(1).AddAsync(Arg.Any<Recommendation>());
    }

    [Fact]
    public async Task GetRecommendationHistory_ShouldReturnFailure_WhenPageIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetRecommendationHistoryQuery(userId, 0, 10);

        // Act
        var result = await _recommendationHistoryHandler.HandleAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Recommendation.History");
    }

    [Fact]
    public async Task GetRecommendationHistory_ShouldReturnSuccess_WithPagedData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var items = new List<Recommendation> { Recommendation.CreateFromScore(userId, analysisId, 80) };
        var pagedList = new PagedList<Recommendation>(items, 1, 10, 1);

        _recommendationRepository.GetPagedByUserIdAsync(userId, 1, 10)
            .Returns(pagedList);

        var query = new GetRecommendationHistoryQuery(userId, 1, 10);

        // Act
        var result = await _recommendationHistoryHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].RecommendationType.Should().Be("ModerateWorkout");
    }
}
