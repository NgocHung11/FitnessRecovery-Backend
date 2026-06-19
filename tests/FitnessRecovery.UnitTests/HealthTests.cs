using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Commands.CreateHealthRecord;
using FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;
using FitnessRecovery.Features.Health.Queries.GetHealthRecord;
using FitnessRecovery.Features.Health.Queries.GetHealthRecordHistory;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class HealthTests
{
    private readonly IHealthRecordRepository _healthRecordRepository = Substitute.For<IHealthRecordRepository>();
    private readonly CreateHealthRecordHandler _createHandler;
    private readonly UpdateHealthRecordHandler _updateHandler;
    private readonly GetHealthRecordHandler _getHandler;
    private readonly GetHealthRecordHistoryHandler _getHistoryHandler;

    public HealthTests()
    {
        _createHandler = new CreateHealthRecordHandler(_healthRecordRepository);
        _updateHandler = new UpdateHealthRecordHandler(_healthRecordRepository);
        _getHandler = new GetHealthRecordHandler(_healthRecordRepository);
        _getHistoryHandler = new GetHealthRecordHistoryHandler(_healthRecordRepository);
    }

    [Fact]
    public void SleepHours_ShouldThrowException_WhenValueIsOutOfRange()
    {
        var action1 = () => new SleepHours(-1.0);
        var action2 = () => new SleepHours(24.5);

        action1.Should().Throw<ArgumentException>().WithMessage("*Sleep hours must be between 0 and 24.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Sleep hours must be between 0 and 24.*");
    }

    [Fact]
    public void HeartRate_ShouldThrowException_WhenValueIsOutOfRange()
    {
        var action1 = () => new HeartRate(15);
        var action2 = () => new HeartRate(260);

        action1.Should().Throw<ArgumentException>().WithMessage("*Heart rate must be between 20 and 250 bpm.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Heart rate must be between 20 and 250 bpm.*");
    }

    [Fact]
    public void Steps_ShouldThrowException_WhenValueIsNegative()
    {
        var action = () => new Steps(-1);

        action.Should().Throw<ArgumentException>().WithMessage("*Steps cannot be negative.*");
    }

    [Fact]
    public void HealthRecord_ShouldThrowException_WhenWeightIsZeroOrNegative()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var action1 = () => new HealthRecord(userId, date, 8.0, SleepQuality.Good, 60, 75, 10000, 0.0, 300);
        var action2 = () => new HealthRecord(userId, date, 8.0, SleepQuality.Good, 60, 75, 10000, -5.0, 300);

        action1.Should().Throw<ArgumentException>().WithMessage("*Weight must be greater than zero.*");
        action2.Should().Throw<ArgumentException>().WithMessage("*Weight must be greater than zero.*");
    }

    [Fact]
    public void HealthRecord_ShouldThrowException_WhenCaloriesBurnedIsNegative()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var action = () => new HealthRecord(userId, date, 8.0, SleepQuality.Good, 60, 75, 10000, 70.0, -100);

        action.Should().Throw<ArgumentException>().WithMessage("*Calories burned cannot be negative.*");
    }

    [Fact]
    public void HealthRecord_ShouldThrowException_WhenDateIsInTheFuture()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var action = () => new HealthRecord(userId, date, 8.0, SleepQuality.Good, 60, 75, 10000, 70.0, 300);

        action.Should().Throw<ArgumentException>().WithMessage("*Record date cannot be in the future.*");
    }

    [Fact]
    public async Task CreateHealthRecordHandler_ShouldCreateRecord_WhenInputIsValid()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var command = new CreateHealthRecordCommand(userId, date, 7.5, "Good", 62, 80, 8500, 72.5, 450);

        _healthRecordRepository.GetByDateAsync(userId, date).Returns((HealthRecord)null!);

        var result = await _createHandler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _healthRecordRepository.Received(1).AddAsync(Arg.Is<HealthRecord>(h =>
            h.UserId == userId &&
            h.RecordDate == date &&
            h.SleepHours.Value == 7.5 &&
            h.SleepQuality == SleepQuality.Good &&
            h.RestingHeartRate.Value == 62 &&
            h.AverageHeartRate.Value == 80 &&
            h.Steps.Value == 8500 &&
            h.Weight == 72.5 &&
            h.CaloriesBurned == 450));
    }

    [Fact]
    public async Task CreateHealthRecordHandler_ShouldReturnFailure_WhenRecordAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var existingRecord = new HealthRecord(userId, date, 8.0, SleepQuality.Excellent, 55, 70, 12000, 70.0, 500);
        _healthRecordRepository.GetByDateAsync(userId, date).Returns(existingRecord);

        var command = new CreateHealthRecordCommand(userId, date, 7.5, "Good", 62, 80, 8500, 72.5, 450);

        var result = await _createHandler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HealthRecord.AlreadyExists");
        await _healthRecordRepository.DidNotReceive().AddAsync(Arg.Any<HealthRecord>());
    }

    [Fact]
    public async Task UpdateHealthRecordHandler_ShouldUpdateRecord_WhenFound()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var record = new HealthRecord(userId, date, 8.0, SleepQuality.Excellent, 55, 70, 12000, 70.0, 500);
        _healthRecordRepository.GetByDateAsync(userId, date).Returns(record);

        var command = new UpdateHealthRecordCommand(userId, date, 6.5, "Poor", 68, 85, 4000, 71.0, 200);

        var result = await _updateHandler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        record.SleepHours.Value.Should().Be(6.5);
        record.SleepQuality.Should().Be(SleepQuality.Poor);
        record.RestingHeartRate.Value.Should().Be(68);
        record.AverageHeartRate.Value.Should().Be(85);
        record.Steps.Value.Should().Be(4000);
        record.Weight.Should().Be(71.0);
        record.CaloriesBurned.Should().Be(200);

        await _healthRecordRepository.Received(1).UpdateAsync(record);
    }

    [Fact]
    public async Task UpdateHealthRecordHandler_ShouldReturnFailure_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _healthRecordRepository.GetByDateAsync(userId, date).Returns((HealthRecord)null!);

        var command = new UpdateHealthRecordCommand(userId, date, 6.5, "Poor", 68, 85, 4000, 71.0, 200);

        var result = await _updateHandler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.NotFound");
        await _healthRecordRepository.DidNotReceive().UpdateAsync(Arg.Any<HealthRecord>());
    }

    [Fact]
    public async Task GetHealthRecordHandler_ShouldReturnDto_WhenFound()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var record = new HealthRecord(userId, date, 8.0, SleepQuality.Excellent, 55, 70, 12000, 70.0, 500);
        _healthRecordRepository.GetByDateAsync(userId, date).Returns(record);

        var query = new GetHealthRecordQuery(userId, date);

        var result = await _getHandler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SleepHours.Should().Be(8.0);
        result.Value.SleepQuality.Should().Be("Excellent");
        result.Value.RestingHeartRate.Should().Be(55);
        result.Value.AverageHeartRate.Should().Be(70);
        result.Value.Steps.Should().Be(12000);
        result.Value.Weight.Should().Be(70.0);
        result.Value.CaloriesBurned.Should().Be(500);
    }

    [Fact]
    public async Task GetHealthRecordHandler_ShouldReturnFailure_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _healthRecordRepository.GetByDateAsync(userId, date).Returns((HealthRecord)null!);

        var query = new GetHealthRecordQuery(userId, date);

        var result = await _getHandler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.NotFound");
    }

    [Fact]
    public async Task GetHealthRecordHistoryHandler_ShouldReturnPagedDtos()
    {
        var userId = Guid.NewGuid();
        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var date2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var records = new List<HealthRecord>
        {
            new(userId, date1, 7.5, SleepQuality.Good, 60, 75, 8000, 70.0, 400),
            new(userId, date2, 8.0, SleepQuality.Excellent, 58, 72, 11000, 69.8, 480)
        };

        var pagedList = new PagedList<HealthRecord>(records, 1, 10, 2);
        _healthRecordRepository.GetPagedByUserIdAsync(userId, 1, 10).Returns(pagedList);

        var query = new GetHealthRecordHistoryQuery(userId, 1, 10);

        var result = await _getHistoryHandler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].RecordDate.Should().Be(date1);
        result.Value.Items[1].RecordDate.Should().Be(date2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItems.Should().Be(2);
    }
}
