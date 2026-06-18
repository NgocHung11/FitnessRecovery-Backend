using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.Extensions.Configuration;

namespace FitnessRecovery.Features.Auth.Commands.Login;

public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ITokenCacheService _tokenCacheService;
    private readonly IConfiguration _configuration;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITokenCacheService tokenCacheService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _tokenCacheService = tokenCacheService;
        _configuration = configuration;
    }

    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(new Error("Auth.InvalidCredentials", "Invalid email or password."));
        }

        // Fetch user with refresh tokens to update aggregate
        var userWithTokens = await _userRepository.GetByUserWithRefreshTokensAsync(user.Id);
        user = userWithTokens ?? user;

        var (accessToken, _, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var durationInDaysStr = _configuration["Jwt:RefreshTokenDurationInDays"] ?? "7";
        _ = double.TryParse(durationInDaysStr, out var durationInDays);
        if (durationInDays <= 0) durationInDays = 7;
        var refreshTokenExpiration = TimeSpan.FromDays(durationInDays);
        var refreshTokenExpiresAt = DateTime.UtcNow.Add(refreshTokenExpiration);

        user.AddRefreshToken(refreshToken, refreshTokenExpiresAt);
        await _userRepository.UpdateAsync(user);

        // Cache active refresh token in Redis
        await _tokenCacheService.CacheRefreshTokenAsync(refreshToken, user.Id, refreshTokenExpiration);

        return Result.Success(new LoginResponse(accessToken, refreshToken, expiresAt));
    }
}
