using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Dashboard.Queries.GetAnalytics;
using FitnessRecovery.Features.Dashboard.Queries.GetDailyDashboard;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class DashboardTests
{
    private readonly IRecoveryRepository _recoveryRepository = Substitute.For<IRecoveryRepository>();
    private readonly IHealthRecordRepository _healthRecordRepository = Substitute.For<IHealthRecordRepository>();
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly IRecommendationRepository _recommendationRepository = Substitute.For<IRecommendationRepository>();
    private readonly IRecoveryAnalysisMongoRepository _recoveryAnalysisMongoRepository = Substitute.For<IRecoveryAnalysisMongoRepository>();
    private readonly IDashboardCacheService _dashboardCacheService = Substitute.For<IDashboardCacheService>();
    private readonly IRecoveryCacheService _recoveryCacheService = Substitute.For<IRecoveryCacheService>();

    private readonly GetTodayRecoveryHandler _getTodayRecoveryHandler;
    private readonly GetDailyDashboardHandler _dailyDashboardHandler;
    private readonly GetAnalyticsHandler _analyticsHandler;

    public DashboardTests()
    {
        _getTodayRecoveryHandler = new GetTodayRecoveryHandler(
            _recoveryRepository,
            _healthRecordRepository,
            _workoutRepository,
            _recommendationRepository,
            _recoveryAnalysisMongoRepository,
            _recoveryCacheService);

        _dailyDashboardHandler = new GetDailyDashboardHandler(
            _workoutRepository,
            _healthRecordRepository,
            _getTodayRecoveryHandler,
            _dashboardCacheService);

        _analyticsHandler = new GetAnalyticsHandler(
            _recoveryRepository,
            _healthRecordRepository,
            _workoutRepository);
    }

    [Fact]
    public async Task GetDailyDashboard_ShouldGracefullyDegrade_WhenHealthRecordIsMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Mock missing health record
        _healthRecordRepository.GetByDateAsync(userId, today).Returns((HealthRecord)null);

        // 2. Mock some workouts for today
        var todayWorkout = new WorkoutSession(userId, WorkoutType.Running, 30, 300, WorkoutIntensity.Moderate, "Run", DateTime.UtcNow);
        _workoutRepository.GetWorkoutsForDateAsync(userId, today).Returns(new List<WorkoutSession> { todayWorkout });

        var query = new GetDailyDashboardQuery(userId);

        // Act
        var result = await _dailyDashboardHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecoveryScore.Should().BeNull();
        result.Value.RecoveryStatus.Should().BeNull();
        result.Value.SleepHours.Should().BeNull();
        result.Value.SleepQuality.Should().BeNull();
        result.Value.Steps.Should().BeNull();
        result.Value.Workouts.Should().HaveCount(1);
        result.Value.Workouts[0].WorkoutType.Should().Be("Running");
    }

    [Fact]
    public async Task GetDailyDashboard_ShouldReturnFullyPopulatedDashboard_WhenHealthRecordExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Mock health record today
        var todayHealth = new HealthRecord(userId, today, 8.0, SleepQuality.Good, 60, 75, 10000, 70.0, 300);
        _healthRecordRepository.GetByDateAsync(userId, today).Returns(todayHealth);

        // 2. Mock yesterday health and workouts for recovery analysis calculation
        _healthRecordRepository.GetByDateAsync(userId, today.AddDays(-1)).Returns((HealthRecord)null);
        _workoutRepository.GetWorkoutsForDateAsync(userId, today.AddDays(-1)).Returns(new List<WorkoutSession>());

        // 3. Mock recovery repository check and upsert behavior
        _recoveryRepository.GetByDateAsync(userId, today).Returns((RecoveryAnalysis)null);
        _recoveryRepository.AddAsync(Arg.Any<RecoveryAnalysis>()).Returns(Task.CompletedTask);

        // 4. Mock workouts for today
        var todayWorkout = new WorkoutSession(userId, WorkoutType.Swimming, 45, 400, WorkoutIntensity.High, "Swim", DateTime.UtcNow);
        _workoutRepository.GetWorkoutsForDateAsync(userId, today).Returns(new List<WorkoutSession> { todayWorkout });

        var query = new GetDailyDashboardQuery(userId);

        // Act
        var result = await _dailyDashboardHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecoveryScore.Should().NotBeNull();
        result.Value.RecoveryStatus.Should().NotBeNull();
        result.Value.SleepHours.Should().Be(8.0);
        result.Value.SleepQuality.Should().Be("Good");
        result.Value.Steps.Should().Be(10000);
        result.Value.Workouts.Should().HaveCount(1);
        result.Value.Workouts[0].WorkoutType.Should().Be("Swimming");
    }

    [Fact]
    public async Task GetAnalytics_ShouldFillGapsAndAggregateDataCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weeklyStartDate = today.AddDays(-6);
        var monthlyStartDate = today.AddDays(-29);

        // 1. Mock recovery analyses for the month (only 1 logged 2 days ago)
        var twoDaysAgo = today.AddDays(-2);
        var analysis = new RecoveryAnalysis(userId, twoDaysAgo, new RecoveryScore(82), RecoveryStatus.Good, 80, 80, 80, 80);
        _recoveryRepository.GetByDateRangeAsync(userId, monthlyStartDate, today)
            .Returns(new List<RecoveryAnalysis> { analysis });

        // 2. Mock health records (only today logged)
        var todayHealth = new HealthRecord(userId, today, 7.5, SleepQuality.Excellent, 55, 78, 12000, 70.0, 300);
        _healthRecordRepository.GetByDateRangeAsync(userId, weeklyStartDate, today)
            .Returns(new List<HealthRecord> { todayHealth });

        // 3. Mock workouts (2 workouts logged yesterday)
        var yesterday = today.AddDays(-1);
        var yesterdayTime = yesterday.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
        var workout1 = new WorkoutSession(userId, WorkoutType.Running, 30, 300, WorkoutIntensity.Moderate, "Run", yesterdayTime);
        var workout2 = new WorkoutSession(userId, WorkoutType.Cycling, 45, 250, WorkoutIntensity.Low, "Bike", yesterdayTime);
        _workoutRepository.GetByDateRangeAsync(userId, weeklyStartDate, today)
            .Returns(new List<WorkoutSession> { workout1, workout2 });

        var query = new GetAnalyticsQuery(userId);

        // Act
        var result = await _analyticsHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Check Weekly Recovery Gaps
        result.Value.WeeklyRecovery.Should().HaveCount(7);
        result.Value.WeeklyRecovery.First(p => p.Date == twoDaysAgo).Score.Should().Be(82);
        result.Value.WeeklyRecovery.First(p => p.Date == today).Score.Should().BeNull();

        // Check Monthly Recovery
        result.Value.MonthlyRecovery.Should().HaveCount(30);
        result.Value.MonthlyRecovery.First(p => p.Date == twoDaysAgo).Score.Should().Be(82);

        // Check Workout Trend Aggregations
        result.Value.WorkoutTrend.Should().HaveCount(7);
        var yesterdayPoint = result.Value.WorkoutTrend.First(p => p.Date == yesterday);
        yesterdayPoint.WorkoutCount.Should().Be(2);
        yesterdayPoint.TotalDurationMinutes.Should().Be(75);
        yesterdayPoint.TotalCaloriesBurned.Should().Be(550);

        // Check Sleep Trend Gaps
        result.Value.SleepTrend.Should().HaveCount(7);
        var todaySleepPoint = result.Value.SleepTrend.First(p => p.Date == today);
        todaySleepPoint.SleepHours.Should().Be(7.5);
        todaySleepPoint.SleepQuality.Should().Be("Excellent");

        var yesterdaySleepPoint = result.Value.SleepTrend.First(p => p.Date == yesterday);
        yesterdaySleepPoint.SleepHours.Should().Be(0);
        yesterdaySleepPoint.SleepQuality.Should().BeNull();
    }
}
