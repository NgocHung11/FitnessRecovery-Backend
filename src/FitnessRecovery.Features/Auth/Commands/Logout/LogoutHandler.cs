using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Auth.Commands.Logout;

public class LogoutHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenCacheService _tokenCacheService;

    public LogoutHandler(IUserRepository userRepository, ITokenCacheService tokenCacheService)
    {
        _userRepository = userRepository;
        _tokenCacheService = tokenCacheService;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand command,
        string? jti,
        TimeSpan? accessTokenRemainingTime,
        CancellationToken cancellationToken = default)
    {
        // 1. Blacklist Access Token JTI
        if (!string.IsNullOrEmpty(jti) && accessTokenRemainingTime.HasValue && accessTokenRemainingTime.Value > TimeSpan.Zero)
        {
            await _tokenCacheService.BlacklistAccessTokenAsync(jti, accessTokenRemainingTime.Value);
        }

        // 2. Invalidate Refresh Token in Cache
        await _tokenCacheService.InvalidateRefreshTokenAsync(command.RefreshToken);

        // 3. Revoke Refresh Token in Database
        var user = await _userRepository.GetByRefreshTokenAsync(command.RefreshToken);
        if (user != null)
        {
            user.RevokeRefreshToken(command.RefreshToken);
            await _userRepository.UpdateAsync(user);
        }

        return Result.Success();
    }
}
