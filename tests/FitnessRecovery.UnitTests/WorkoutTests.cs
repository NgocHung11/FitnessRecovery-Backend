using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Commands.CreateWorkout;
using FitnessRecovery.Features.Workout.Commands.UpdateWorkout;
using FitnessRecovery.Features.Workout.Commands.DeleteWorkout;
using FitnessRecovery.Features.Workout.Queries.GetWorkout;
using FitnessRecovery.Features.Workout.Queries.GetWorkoutHistory;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class WorkoutTests
{
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly IDashboardCacheService _dashboardCacheService = Substitute.For<IDashboardCacheService>();
    private readonly IRecoveryCacheService _recoveryCacheService = Substitute.For<IRecoveryCacheService>();
    private readonly CreateWorkoutHandler _createHandler;
    private readonly UpdateWorkoutHandler _updateHandler;
    private readonly DeleteWorkoutHandler _deleteHandler;
    private readonly GetWorkoutHandler _getHandler;
    private readonly GetWorkoutHistoryHandler _getHistoryHandler;

    public WorkoutTests()
    {
        _createHandler = new CreateWorkoutHandler(_workoutRepository, _recoveryCacheService, _dashboardCacheService);
        _updateHandler = new UpdateWorkoutHandler(_workoutRepository, _recoveryCacheService, _dashboardCacheService);
        _deleteHandler = new DeleteWorkoutHandler(_workoutRepository, _recoveryCacheService, _dashboardCacheService);
        _getHandler = new GetWorkoutHandler(_workoutRepository);
        _getHistoryHandler = new GetWorkoutHistoryHandler(_workoutRepository);
    }

    [Fact]
    public void WorkoutSession_ShouldThrowException_WhenDurationIsZeroOrNegative()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action1 = () => new WorkoutSession(userId, WorkoutType.Running, 0, 100, WorkoutIntensity.Moderate, null, DateTime.UtcNow);
        var action2 = () => new WorkoutSession(userId, WorkoutType.Running, -5, 100, WorkoutIntensity.Moderate, null, DateTime.UtcNow);

        // Act & Assert
        action1.Should().Throw<ArgumentException>().WithMessage("*Duration must be greater than zero.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Duration must be greater than zero.*");
    }

    [Fact]
    public void WorkoutSession_ShouldThrowException_WhenCaloriesAreNegative()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = () => new WorkoutSession(userId, WorkoutType.Running, 30, -1, WorkoutIntensity.Moderate, null, DateTime.UtcNow);

        // Act & Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Calories burned cannot be negative.*");
    }

    [Fact]
    public void WorkoutSession_ShouldThrowException_WhenDateIsInTheFuture()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = () => new WorkoutSession(userId, WorkoutType.Running, 30, 100, WorkoutIntensity.Moderate, null, DateTime.UtcNow.AddDays(1));

        // Act & Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Workout date cannot be in the future.*");
    }

    [Fact]
    public async Task CreateWorkoutHandler_ShouldCreateWorkout_WhenInputIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateWorkoutCommand(userId, WorkoutType.Cycling, 45, 400, WorkoutIntensity.High, "Nice weather", DateTime.UtcNow.AddHours(-1));

        // Act
        var result = await _createHandler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _workoutRepository.Received(1).AddAsync(Arg.Is<WorkoutSession>(w =>
            w.UserId == userId &&
            w.WorkoutType == WorkoutType.Cycling &&
            w.DurationMinutes == 45 &&
            w.CaloriesBurned == 400 &&
            w.Intensity == WorkoutIntensity.High &&
            w.Notes == "Nice weather"));
    }

    [Fact]
    public async Task UpdateWorkoutHandler_ShouldUpdateWorkout_WhenOwnerAndInputIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new WorkoutSession(userId, WorkoutType.Running, 30, 300, WorkoutIntensity.Moderate, "Old notes", DateTime.UtcNow.AddHours(-2));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var command = new UpdateWorkoutCommand(session.Id, userId, WorkoutType.Running, 40, 400, WorkoutIntensity.High, "New notes", DateTime.UtcNow.AddHours(-1));

        // Act
        var result = await _updateHandler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.DurationMinutes.Should().Be(40);
        session.CaloriesBurned.Should().Be(400);
        session.Intensity.Should().Be(WorkoutIntensity.High);
        session.Notes.Should().Be("New notes");

        await _workoutRepository.Received(1).UpdateAsync(session);
    }

    [Fact]
    public async Task UpdateWorkoutHandler_ShouldReturnError_WhenUserIsNotOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = new WorkoutSession(ownerId, WorkoutType.Running, 30, 300, WorkoutIntensity.Moderate, "Notes", DateTime.UtcNow.AddHours(-2));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var command = new UpdateWorkoutCommand(session.Id, otherUserId, WorkoutType.Running, 40, 400, WorkoutIntensity.High, "New notes", DateTime.UtcNow.AddHours(-1));

        // Act
        var result = await _updateHandler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workout.Unauthorized");

        await _workoutRepository.DidNotReceive().UpdateAsync(Arg.Any<WorkoutSession>());
    }

    [Fact]
    public async Task DeleteWorkoutHandler_ShouldDeleteWorkout_WhenOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new WorkoutSession(userId, WorkoutType.Swimming, 60, 500, WorkoutIntensity.High, "Swim session", DateTime.UtcNow.AddHours(-3));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var command = new DeleteWorkoutCommand(session.Id, userId);

        // Act
        var result = await _deleteHandler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _workoutRepository.Received(1).DeleteAsync(session);
    }

    [Fact]
    public async Task DeleteWorkoutHandler_ShouldReturnError_WhenNotOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = new WorkoutSession(ownerId, WorkoutType.Swimming, 60, 500, WorkoutIntensity.High, "Swim session", DateTime.UtcNow.AddHours(-3));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var command = new DeleteWorkoutCommand(session.Id, otherUserId);

        // Act
        var result = await _deleteHandler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workout.Unauthorized");
        await _workoutRepository.DidNotReceive().DeleteAsync(Arg.Any<WorkoutSession>());
    }

    [Fact]
    public async Task GetWorkoutHandler_ShouldReturnWorkout_WhenOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new WorkoutSession(userId, WorkoutType.Gym, 90, 600, WorkoutIntensity.High, "Leg day", DateTime.UtcNow.AddHours(-1));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var query = new GetWorkoutQuery(session.Id, userId);

        // Act
        var result = await _getHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Notes.Should().Be("Leg day");
        result.Value.WorkoutType.Should().Be("Gym");
    }

    [Fact]
    public async Task GetWorkoutHandler_ShouldReturnError_WhenNotOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = new WorkoutSession(ownerId, WorkoutType.Gym, 90, 600, WorkoutIntensity.High, "Leg day", DateTime.UtcNow.AddHours(-1));
        _workoutRepository.GetByIdAsync(session.Id).Returns(session);

        var query = new GetWorkoutQuery(session.Id, otherUserId);

        // Act
        var result = await _getHandler.HandleAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workout.Unauthorized");
    }

    [Fact]
    public async Task GetWorkoutHistoryHandler_ShouldReturnPagedWorkouts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<WorkoutSession>
        {
            new(userId, WorkoutType.Running, 30, 300, WorkoutIntensity.Moderate, "Run", DateTime.UtcNow.AddDays(-1)),
            new(userId, WorkoutType.Walking, 20, 100, WorkoutIntensity.Low, "Walk", DateTime.UtcNow)
        };

        var pagedList = new PagedList<WorkoutSession>(sessions, 1, 10, 2);
        _workoutRepository.GetPagedByUserIdAsync(userId, 1, 10).Returns(pagedList);

        var query = new GetWorkoutHistoryQuery(userId, 1, 10);

        // Act
        var result = await _getHistoryHandler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Notes.Should().Be("Run");
        result.Value.Items[1].Notes.Should().Be("Walk");
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItems.Should().Be(2);
    }
}
