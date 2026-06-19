using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class RedisCacheTests
{
    private readonly IDistributedCache _mockCache = Substitute.For<IDistributedCache>();

    [Fact]
    public async Task DashboardCacheService_GetDailyDashboardAsync_ShouldReturnNull_WhenCacheIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"dashboard:daily:{userId}";
        _mockCache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var service = new DashboardCacheService(_mockCache);

        // Act
        var result = await service.GetDailyDashboardAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DashboardCacheService_GetDailyDashboardAsync_ShouldReturnDashboard_WhenCacheExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"dashboard:daily:{userId}";
        var dashboard = new DailyDashboardDto(80, "Good", 7.5, "Good", 10000, 10000, new List<DashboardWorkoutDto>());
        var json = JsonSerializer.Serialize(dashboard);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        _mockCache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns(bytes);

        var service = new DashboardCacheService(_mockCache);

        // Act
        var result = await service.GetDailyDashboardAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.RecoveryScore.Should().Be(80);
        result.RecoveryStatus.Should().Be("Good");
        result.SleepHours.Should().Be(7.5);
    }

    [Fact]
    public async Task DashboardCacheService_SetDailyDashboardAsync_ShouldSerializeAndSetCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"dashboard:daily:{userId}";
        var dashboard = new DailyDashboardDto(80, "Good", 7.5, "Good", 10000, 10000, new List<DashboardWorkoutDto>());
        
        var service = new DashboardCacheService(_mockCache);

        // Act
        await service.SetDailyDashboardAsync(userId, dashboard, TimeSpan.FromHours(1));

        // Assert
        await _mockCache.Received(1).SetAsync(
            cacheKey,
            Arg.Is<byte[]>(b => JsonSerializer.Deserialize<DailyDashboardDto>(System.Text.Encoding.UTF8.GetString(b))!.RecoveryScore == 80),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DashboardCacheService_InvalidateDailyDashboardAsync_ShouldRemoveKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"dashboard:daily:{userId}";
        
        var service = new DashboardCacheService(_mockCache);

        // Act
        await service.InvalidateDailyDashboardAsync(userId);

        // Assert
        await _mockCache.Received(1).RemoveAsync(cacheKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecoveryCacheService_GetTodayRecoveryAsync_ShouldReturnNull_WhenCacheIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"recovery:today:{userId}";
        _mockCache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var service = new RecoveryCacheService(_mockCache);

        // Act
        var result = await service.GetTodayRecoveryAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RecoveryCacheService_GetTodayRecoveryAsync_ShouldReturnRecovery_WhenCacheExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"recovery:today:{userId}";
        var analysis = new RecoveryAnalysisDto(Guid.NewGuid(), userId, new DateOnly(2026, 6, 19), 85, "Good", 80, 85, 90, 95, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(analysis);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        _mockCache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns(bytes);

        var service = new RecoveryCacheService(_mockCache);

        // Act
        var result = await service.GetTodayRecoveryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.RecoveryScore.Should().Be(85);
        result.RecoveryStatus.Should().Be("Good");
    }
}
