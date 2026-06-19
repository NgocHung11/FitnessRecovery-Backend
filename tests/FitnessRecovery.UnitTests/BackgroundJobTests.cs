using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Domain;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Dashboard.Domain;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Infrastructure.BackgroundJobs;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class BackgroundJobTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRecoveryRepository _recoveryRepository = Substitute.For<IRecoveryRepository>();
    private readonly IHealthRecordRepository _healthRecordRepository = Substitute.For<IHealthRecordRepository>();
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly IWeeklyReportMongoRepository _weeklyReportMongoRepository = Substitute.For<IWeeklyReportMongoRepository>();
    private readonly IDashboardCacheService _dashboardCacheService = Substitute.For<IDashboardCacheService>();
    private readonly IRecommendationRepository _recommendationRepository = Substitute.For<IRecommendationRepository>();
    private readonly IRecoveryAnalysisMongoRepository _recoveryAnalysisMongoRepository = Substitute.For<IRecoveryAnalysisMongoRepository>();
    private readonly IRecoveryCacheService _recoveryCacheService = Substitute.For<IRecoveryCacheService>();

    private readonly GetTodayRecoveryHandler _todayRecoveryHandler;

    public BackgroundJobTests()
    {
        _todayRecoveryHandler = new GetTodayRecoveryHandler(
            _recoveryRepository,
            _healthRecordRepository,
            _workoutRepository,
            _recommendationRepository,
            _recoveryAnalysisMongoRepository,
            _recoveryCacheService);
    }

    [Fact]
    public async Task RecoveryCalculationJob_ShouldRunHandler_OnlyForUsersWithoutRecoveryScoreToday()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        var users = new List<User> { user1, user2 };

        _userRepository.GetAllUsersAsync().Returns(users);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        // User1 already has recovery analysis for today
        var user1Analysis = new RecoveryAnalysis(user1.Id, today, 80, RecoveryStatus.Good, 80, 80, 80, 80);
        _recoveryRepository.GetByDateAsync(user1.Id, today).Returns(user1Analysis);

        // User2 does NOT have recovery analysis for today
        _recoveryRepository.GetByDateAsync(user2.Id, today).Returns((RecoveryAnalysis)null!);

        // Mock health records for User2 calculation
        var user2HealthToday = new HealthRecord(user2.Id, today, 8.0, SleepQuality.Good, 60, 75, 5000, 70.0, 300);
        _healthRecordRepository.GetByDateAsync(user2.Id, today).Returns(user2HealthToday);
        _healthRecordRepository.GetByDateAsync(user2.Id, yesterday).Returns((HealthRecord)null!);
        _workoutRepository.GetWorkoutsForDateAsync(user2.Id, yesterday).Returns(new List<WorkoutSession>());

        var logger = Substitute.For<ILogger<RecoveryCalculationJob>>();
        var job = new RecoveryCalculationJob(_userRepository, _recoveryRepository, _todayRecoveryHandler, logger);

        // Act
        await job.RunAsync();

        // Assert
        // Recovery calculation should be skipped for User1
        await _recoveryRepository.Received(0).AddAsync(Arg.Is<RecoveryAnalysis>(a => a.UserId == user1.Id));

        // Recovery calculation should run and add new analysis for User2
        await _recoveryRepository.Received(1).AddAsync(Arg.Is<RecoveryAnalysis>(a => a.UserId == user2.Id));
    }

    [Fact]
    public async Task WeeklyReportJob_ShouldCompileAndSaveReports_AndClearCache()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepository.GetAllUsersAsync().Returns(new List<User> { user });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Completed week Monday to Sunday
        int diff = (7 + (today.AddDays(-1).DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = today.AddDays(-1).AddDays(-1 * diff);
        var endOfWeek = startOfWeek.AddDays(6);

        // Seed some data for that week
        var healthRecords = new List<HealthRecord>
        {
            new(user.Id, startOfWeek, 8.0, SleepQuality.Good, 60, 70, 8000, 70.0, 300),
            new(user.Id, startOfWeek.AddDays(1), 7.0, SleepQuality.Average, 65, 75, 12000, 70.0, 300)
        };
        _healthRecordRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(healthRecords);

        var analyses = new List<RecoveryAnalysis>
        {
            new(user.Id, startOfWeek, 80, RecoveryStatus.Good, 80, 80, 80, 80),
            new(user.Id, startOfWeek.AddDays(1), 90, RecoveryStatus.Excellent, 90, 90, 90, 90)
        };
        _recoveryRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(analyses);

        var workouts = new List<WorkoutSession>
        {
            new(user.Id, WorkoutType.Running, 45, 450, WorkoutIntensity.Moderate, "Run", startOfWeek.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
        };
        _workoutRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(workouts);

        var logger = Substitute.For<ILogger<WeeklyReportJob>>();
        var job = new WeeklyReportJob(
            _userRepository,
            _healthRecordRepository,
            _recoveryRepository,
            _workoutRepository,
            _weeklyReportMongoRepository,
            _dashboardCacheService,
            logger);

        // Act
        await job.RunAsync();

        // Assert
        // Verify aggregation values:
        // Average Recovery: (80 + 90) / 2 = 85
        // Average Sleep: (8 + 7) / 2 = 7.5
        // Total Duration: 45
        // Total Calories: 450
        // Total Steps: 8000 + 12000 = 20000
        await _weeklyReportMongoRepository.Received(1).UpsertAsync(Arg.Is<WeeklyReport>(r =>
            r.UserId == user.Id &&
            r.StartDate == startOfWeek &&
            r.EndDate == endOfWeek &&
            Math.Abs(r.AverageRecoveryScore - 85.0) < 0.01 &&
            Math.Abs(r.AverageSleepHours - 7.5) < 0.01 &&
            r.TotalWorkoutDuration == 45 &&
            r.TotalCaloriesBurned == 450 &&
            r.TotalSteps == 20000));

        await _dashboardCacheService.Received(1).InvalidateWeeklyReportsAsync(user.Id);
    }

    [Fact]
    public async Task WeeklyReportJob_ShouldSkipUser_WhenNoDataExistsForWeek()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepository.GetAllUsersAsync().Returns(new List<User> { user });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int diff = (7 + (today.AddDays(-1).DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = today.AddDays(-1).AddDays(-1 * diff);
        var endOfWeek = startOfWeek.AddDays(6);

        _healthRecordRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(new List<HealthRecord>());
        _recoveryRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(new List<RecoveryAnalysis>());
        _workoutRepository.GetByDateRangeAsync(user.Id, startOfWeek, endOfWeek).Returns(new List<WorkoutSession>());

        var logger = Substitute.For<ILogger<WeeklyReportJob>>();
        var job = new WeeklyReportJob(
            _userRepository,
            _healthRecordRepository,
            _recoveryRepository,
            _workoutRepository,
            _weeklyReportMongoRepository,
            _dashboardCacheService,
            logger);

        // Act
        await job.RunAsync();

        // Assert
        await _weeklyReportMongoRepository.Received(0).UpsertAsync(Arg.Any<WeeklyReport>());
        await _dashboardCacheService.Received(0).InvalidateWeeklyReportsAsync(user.Id);
    }

    [Fact]
    public async Task DatabaseCleanupJob_ShouldRemoveExpiredAndRevokedTokens_WhileKeepingActiveTokens()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var userId = Guid.NewGuid();
        var expiredToken = new RefreshToken(userId, "expired-token", DateTime.UtcNow.AddDays(-1));
        
        var revokedToken = new RefreshToken(userId, "revoked-token", DateTime.UtcNow.AddDays(1));
        revokedToken.Revoke();

        var activeToken = new RefreshToken(userId, "active-token", DateTime.UtcNow.AddDays(1));

        using (var context = new ApplicationDbContext(options))
        {
            await context.RefreshTokens.AddRangeAsync(expiredToken, revokedToken, activeToken);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new ApplicationDbContext(options))
        {
            var logger = Substitute.For<ILogger<DatabaseCleanupJob>>();
            var job = new DatabaseCleanupJob(context, logger);
            await job.RunAsync();
        }

        // Assert
        using (var context = new ApplicationDbContext(options))
        {
            var tokens = await context.RefreshTokens.ToListAsync();
            tokens.Should().ContainSingle();
            tokens[0].Token.Should().Be("active-token");
        }
    }

    private static User CreateTestUser()
    {
        return new User(
            Guid.NewGuid().ToString() + "@example.com",
            "passwordHash",
            "FirstName",
            "LastName",
            "Male",
            DateTime.UtcNow.AddYears(-30),
            175.0,
            70.0,
            "Maintain weight"
        );
    }
}
