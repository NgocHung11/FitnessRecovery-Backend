using FitnessRecovery.Features.Auth.Commands.Login;
using FitnessRecovery.Features.Auth.Commands.RefreshToken;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Domain;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace FitnessRecovery.UnitTests;

public class AuthTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ITokenCacheService _tokenCacheService = Substitute.For<ITokenCacheService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    private readonly LoginHandler _loginHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;

    public AuthTests()
    {
        _loginHandler = new LoginHandler(
            _userRepository,
            _passwordHasher,
            _tokenService,
            _tokenCacheService,
            _configuration);

        _refreshTokenHandler = new RefreshTokenHandler(
            _userRepository,
            _tokenService,
            _tokenCacheService,
            _configuration);
            
        _configuration["Jwt:RefreshTokenDurationInDays"].Returns("7");
    }

    [Fact]
    public async Task LoginHandler_ShouldGenerateAndCacheTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var command = new LoginCommand("test@fitness.local", "Password123!");
        var user = new User(
            command.Email,
            "hashed_password",
            "Test",
            "User",
            "Male",
            DateTime.UtcNow.AddYears(-20),
            180.0,
            75.0,
            "BuildMuscle");

        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _userRepository.GetByUserWithRefreshTokensAsync(user.Id).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);

        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        _tokenService.GenerateAccessToken(user).Returns(("access_token_123", "jti_123", expiresAt));
        _tokenService.GenerateRefreshToken().Returns("refresh_token_123");

        // Act
        var result = await _loginHandler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token_123");
        result.Value.RefreshToken.Should().Be("refresh_token_123");

        user.RefreshTokens.Should().ContainSingle(rt => rt.Token == "refresh_token_123");

        await _userRepository.Received(1).UpdateAsync(user);
        await _tokenCacheService.Received(1).CacheRefreshTokenAsync(
            "refresh_token_123",
            user.Id,
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task RefreshTokenHandler_ShouldRotateTokensAndCacheNewToken_WhenTokenIsValid()
    {
        // Arrange
        var oldToken = "old_refresh_token";
        var newToken = "new_refresh_token";
        var command = new RefreshTokenCommand(oldToken);
        
        var user = new User(
            "test@fitness.local",
            "hashed_password",
            "Test",
            "User",
            "Male",
            DateTime.UtcNow.AddYears(-20),
            180.0,
            75.0,
            "BuildMuscle");

        // Add active refresh token to user
        user.AddRefreshToken(oldToken, DateTime.UtcNow.AddDays(1));

        _tokenCacheService.GetCachedRefreshTokenUserIdAsync(oldToken).Returns(user.Id);
        _userRepository.GetByUserWithRefreshTokensAsync(user.Id).Returns(user);

        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        _tokenService.GenerateAccessToken(user).Returns(("access_token_new", "jti_new", expiresAt));
        _tokenService.GenerateRefreshToken().Returns(newToken);

        // Act
        var result = await _refreshTokenHandler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token_new");
        result.Value.RefreshToken.Should().Be(newToken);

        // Verify old token is revoked, new token is added
        user.RefreshTokens.Should().Contain(rt => rt.Token == oldToken && rt.RevokedAt != null);
        user.RefreshTokens.Should().Contain(rt => rt.Token == newToken && rt.RevokedAt == null);

        await _userRepository.Received(1).UpdateAsync(user);
        await _tokenCacheService.Received(1).InvalidateRefreshTokenAsync(oldToken);
        await _tokenCacheService.Received(1).CacheRefreshTokenAsync(newToken, user.Id, Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task RefreshTokenHandler_ShouldRevokeAllTokensAndFail_WhenTokenIsAlreadyRevoked()
    {
        // Arrange
        var oldToken = "already_revoked_token";
        var command = new RefreshTokenCommand(oldToken);
        
        var user = new User(
            "test@fitness.local",
            "hashed_password",
            "Test",
            "User",
            "Male",
            DateTime.UtcNow.AddYears(-20),
            180.0,
            75.0,
            "BuildMuscle");

        // Add a revoked token and an active token
        user.AddRefreshToken(oldToken, DateTime.UtcNow.AddDays(1));
        user.RevokeRefreshToken(oldToken); // Make it inactive
        
        var activeToken = "active_token";
        user.AddRefreshToken(activeToken, DateTime.UtcNow.AddDays(2));

        _tokenCacheService.GetCachedRefreshTokenUserIdAsync(oldToken).Returns(user.Id);
        _userRepository.GetByUserWithRefreshTokensAsync(user.Id).Returns(user);

        // Act
        var result = await _refreshTokenHandler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.TokenReused");

        // Verify all refresh tokens are revoked
        user.RefreshTokens.Should().OnlyContain(rt => rt.RevokedAt != null);

        await _userRepository.Received(1).UpdateAsync(user);
        await _tokenCacheService.Received(1).InvalidateRefreshTokenAsync(oldToken);
        await _tokenCacheService.Received(1).InvalidateRefreshTokenAsync(activeToken);
    }
}
