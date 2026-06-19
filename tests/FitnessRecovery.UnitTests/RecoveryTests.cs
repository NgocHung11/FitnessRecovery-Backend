using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.Features.Recovery.Queries.GetRecoveryHistory;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class RecoveryTests
{
    private readonly IRecoveryRepository _recoveryRepository = Substitute.For<IRecoveryRepository>();
    private readonly IHealthRecordRepository _healthRecordRepository = Substitute.For<IHealthRecordRepository>();
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly GetTodayRecoveryHandler _todayHandler;
    private readonly GetRecoveryHistoryHandler _historyHandler;

    public RecoveryTests()
    {
        _todayHandler = new GetTodayRecoveryHandler(_recoveryRepository, _healthRecordRepository, _workoutRepository);
        _historyHandler = new GetRecoveryHistoryHandler(_recoveryRepository);
    }

    [Fact]
    public void RecoveryScore_ShouldThrowException_WhenValueIsOutOfRange()
    {
        var action1 = () => new RecoveryScore(-1);
        var action2 = () => new RecoveryScore(101);

        action1.Should().Throw<ArgumentException>().WithMessage("*Recovery score must be between 0 and 100.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Recovery score must be between 0 and 100.*");
    }

    [Fact]
    public void RecoveryAnalysis_ShouldThrowException_WhenSubScoresAreOutOfRange()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var score = new RecoveryScore(80);

        var action1 = () => new RecoveryAnalysis(userId, date, score, RecoveryStatus.Good, -5, 80, 80, 80);
        var action2 = () => new RecoveryAnalysis(userId, date, score, RecoveryStatus.Good, 80, 105, 80, 80);

        action1.Should().Throw<ArgumentException>().WithMessage("*Sleep score must be between 0 and 100.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Heart rate score must be between 0 and 100.*");
    }

    [Fact]
    public void RecoveryAnalysis_ShouldThrowException_WhenDateIsInTheFuture()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var score = new RecoveryScore(80);

        var action = () => new RecoveryAnalysis(userId, date, score, RecoveryStatus.Good, 80, 80, 80, 80);

        action.Should().Throw<ArgumentException>().WithMessage("*Analysis date cannot be in the future.*");
    }

    [Fact]
    public void Calculate_ShouldReturnPerfectScore_WhenMetricsAreIdeal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ideal Health Record: 8h sleep, Excellent quality, 50 resting heart rate, 80 average heart rate, 5k steps
        var todayHealth = new HealthRecord(userId, today, 8.0, SleepQuality.Excellent, 50, 80, 5000, 70.0, 300);

        // Yesterday Health Record: 10,000 steps
        var yesterdayHealth = new HealthRecord(userId, today.AddDays(-1), 8.0, SleepQuality.Good, 60, 75, 10000, 70.0, 300);

        // Yesterday Workouts: None
        var yesterdayWorkouts = new List<WorkoutSession>();

        // Act
        var analysis = RecoveryAnalysis.Calculate(userId, today, todayHealth, yesterdayHealth, yesterdayWorkouts);

        // Assert
        // Sleep score = (100 * 0.7) + (100 * 0.3) = 100
        // Heart rate score = 100 - (50 - 50)*2 = 100
        // Workout load score = 100 (no workouts)
        // Activity score = 10000 / 10000 * 100 = 100
        // Final score = 100
        analysis.RecoveryScore.Value.Should().Be(100);
        analysis.RecoveryStatus.Should().Be(RecoveryStatus.Excellent);
        analysis.SleepScore.Should().Be(100);
        analysis.HeartRateScore.Should().Be(100);
        analysis.WorkoutLoadScore.Should().Be(100);
        analysis.ActivityScore.Should().Be(100);
    }

    [Fact]
    public void Calculate_ShouldCalculateCorrectly_UnderHeavyFatigue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Today Health Record: 5h sleep, Poor quality, 80 resting heart rate, 90 average heart rate, 2k steps
        var todayHealth = new HealthRecord(userId, today, 5.0, SleepQuality.Poor, 80, 90, 2000, 70.0, 300);

        // Yesterday Health Record: 4000 steps
        var yesterdayHealth = new HealthRecord(userId, today.AddDays(-1), 7.0, SleepQuality.Average, 65, 75, 4000, 70.0, 300);

        // Yesterday Workouts: 60 mins High intensity workout -> load = 60 * 2 = 120
        var yesterdayWorkouts = new List<WorkoutSession>
        {
            new(userId, WorkoutType.Running, 60, 600, WorkoutIntensity.High, "Hard run", DateTime.UtcNow.AddDays(-1))
        };

        // Act
        var analysis = RecoveryAnalysis.Calculate(userId, today, todayHealth, yesterdayHealth, yesterdayWorkouts);

        // Assert
        // Sleep score: SleepHoursScore = 5/8 * 100 = 62.5. QualityScore = 40. SleepScore = 62.5 * 0.7 + 40 * 0.3 = 43.75 + 12 = 55.75 (~56)
        // Heart rate score: 100 - (80 - 50)*2 = 40
        // Workout load score: max(0, 100 - 120) = 0
        // Activity score: 4000 / 10000 * 100 = 40
        // Final score: (55.75 * 0.4) + (40 * 0.3) + (0 * 0.2) + (40 * 0.1) = 22.3 + 12 + 0 + 4 = 38.3 (~38)
        analysis.RecoveryScore.Value.Should().Be(38);
        analysis.RecoveryStatus.Should().Be(RecoveryStatus.Poor);
        analysis.SleepScore.Should().Be(56);
        analysis.HeartRateScore.Should().Be(40);
        analysis.WorkoutLoadScore.Should().Be(0);
        analysis.ActivityScore.Should().Be(40);
    }

    [Fact]
    public async Task GetTodayRecoveryHandler_ShouldReturnFailure_WhenTodayHealthRecordMissing()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _healthRecordRepository.GetByDateAsync(userId, today).Returns((HealthRecord)null!);

        var query = new GetTodayRecoveryQuery(userId);

        var result = await _todayHandler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Recovery.HealthRecordMissing");
    }

    [Fact]
    public async Task GetTodayRecoveryHandler_ShouldCreateNewAnalysis_WhenNotExists()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var todayHealth = new HealthRecord(userId, today, 8.0, SleepQuality.Good, 60, 75, 5000, 70.0, 300);
        _healthRecordRepository.GetByDateAsync(userId, today).Returns(todayHealth);
        _healthRecordRepository.GetByDateAsync(userId, yesterday).Returns((HealthRecord)null!);
        _workoutRepository.GetWorkoutsForDateAsync(userId, yesterday).Returns(new List<WorkoutSession>());

        _recoveryRepository.GetByDateAsync(userId, today).Returns((RecoveryAnalysis)null!);

        var query = new GetTodayRecoveryQuery(userId);

        var result = await _todayHandler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        await _recoveryRepository.Received(1).AddAsync(Arg.Any<RecoveryAnalysis>());
    }

    [Fact]
    public async Task GetTodayRecoveryHandler_ShouldUpdateExistingAnalysis_WhenAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var todayHealth = new HealthRecord(userId, today, 8.0, SleepQuality.Good, 60, 75, 5000, 70.0, 300);
        _healthRecordRepository.GetByDateAsync(userId, today).Returns(todayHealth);
        _healthRecordRepository.GetByDateAsync(userId, yesterday).Returns((HealthRecord)null!);
        _workoutRepository.GetWorkoutsForDateAsync(userId, yesterday).Returns(new List<WorkoutSession>());

        var existingAnalysis = new RecoveryAnalysis(userId, today, 50, RecoveryStatus.Moderate, 50, 50, 50, 50);
        _recoveryRepository.GetByDateAsync(userId, today).Returns(existingAnalysis);

        var query = new GetTodayRecoveryQuery(userId);

        var result = await _todayHandler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        await _recoveryRepository.Received(1).UpdateAsync(existingAnalysis);
    }

    [Fact]
    public async Task GetRecoveryHistoryHandler_ShouldReturnPagedAnalyses()
    {
        var userId = Guid.NewGuid();
        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var date2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var analyses = new List<RecoveryAnalysis>
        {
            new(userId, date1, 80, RecoveryStatus.Good, 80, 80, 80, 80),
            new(userId, date2, 92, RecoveryStatus.Excellent, 90, 95, 90, 90)
        };

        var pagedList = new PagedList<RecoveryAnalysis>(analyses, 1, 10, 2);
        _recoveryRepository.GetPagedByUserIdAsync(userId, 1, 10).Returns(pagedList);

        var query = new GetRecoveryHistoryQuery(userId, 1, 10);

        var result = await _historyHandler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].RecoveryScore.Should().Be(80);
        result.Value.Items[1].RecoveryScore.Should().Be(92);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItems.Should().Be(2);
    }
}
