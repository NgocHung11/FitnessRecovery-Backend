using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Domain;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.Extensions.Configuration;

namespace FitnessRecovery.Features.Auth.Commands.RefreshToken;

public class RefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITokenCacheService _tokenCacheService;
    private readonly IConfiguration _configuration;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        ITokenCacheService tokenCacheService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _tokenCacheService = tokenCacheService;
        _configuration = configuration;
    }

    public async Task<Result<LoginResponse>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        Guid? userId = await _tokenCacheService.GetCachedRefreshTokenUserIdAsync(command.RefreshToken);
        User? user = null;

        if (userId != null)
        {
            user = await _userRepository.GetByUserWithRefreshTokensAsync(userId.Value);
        }
        else
        {
            // Cache miss - fallback to PostgreSQL
            user = await _userRepository.GetByRefreshTokenAsync(command.RefreshToken);
        }

        if (user == null)
        {
            return Result.Failure<LoginResponse>(new Error("Auth.InvalidRefreshToken", "Invalid refresh token."));
        }

        // Validate token status from User aggregate (source of truth)
        var oldToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == command.RefreshToken);
        if (oldToken == null || !oldToken.IsActive)
        {
            // Security alert: If refresh token is reused, revoke all active tokens.
            foreach (var rt in user.RefreshTokens)
            {
                rt.Revoke();
                await _tokenCacheService.InvalidateRefreshTokenAsync(rt.Token);
            }
            await _userRepository.UpdateAsync(user);
            return Result.Failure<LoginResponse>(new Error("Auth.TokenReused", "Security warning: Refresh token has already been used. Please log in again."));
        }

        // Generate new pair
        var (accessToken, _, expiresAt) = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var durationInDaysStr = _configuration["Jwt:RefreshTokenDurationInDays"] ?? "7";
        _ = double.TryParse(durationInDaysStr, out var durationInDays);
        if (durationInDays <= 0) durationInDays = 7;
        var refreshTokenExpiration = TimeSpan.FromDays(durationInDays);
        var refreshTokenExpiresAt = DateTime.UtcNow.Add(refreshTokenExpiration);

        // Rotate
        user.RevokeRefreshToken(command.RefreshToken);
        user.AddRefreshToken(newRefreshToken, refreshTokenExpiresAt);

        await _userRepository.UpdateAsync(user);

        // Update Cache
        await _tokenCacheService.InvalidateRefreshTokenAsync(command.RefreshToken);
        await _tokenCacheService.CacheRefreshTokenAsync(newRefreshToken, user.Id, refreshTokenExpiration);

        return Result.Success(new LoginResponse(accessToken, newRefreshToken, expiresAt));
    }
}
