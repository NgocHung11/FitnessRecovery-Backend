using System;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using Microsoft.Extensions.Logging;

namespace FitnessRecovery.Infrastructure.BackgroundJobs;

public class RecoveryCalculationJob
{
    private readonly IUserRepository _userRepository;
    private readonly IRecoveryRepository _recoveryRepository;
    private readonly GetTodayRecoveryHandler _todayRecoveryHandler;
    private readonly ILogger<RecoveryCalculationJob> _logger;

    public RecoveryCalculationJob(
        IUserRepository userRepository,
        IRecoveryRepository recoveryRepository,
        GetTodayRecoveryHandler todayRecoveryHandler,
        ILogger<RecoveryCalculationJob> logger)
    {
        _userRepository = userRepository;
        _recoveryRepository = recoveryRepository;
        _todayRecoveryHandler = todayRecoveryHandler;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting daily Recovery Calculation Job...");

        var users = await _userRepository.GetAllUsersAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var user in users)
        {
            try
            {
                var existing = await _recoveryRepository.GetByDateAsync(user.Id, today);
                if (existing is null)
                {
                    _logger.LogInformation("Calculating today's recovery score for user: {UserId}", user.Id);
                    var query = new GetTodayRecoveryQuery(user.Id);
                    var result = await _todayRecoveryHandler.HandleAsync(query);

                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("Successfully calculated recovery score for user: {UserId}. Score: {Score}", user.Id, result.Value.RecoveryScore);
                    }
                    else
                    {
                        _logger.LogWarning("Could not calculate recovery score for user {UserId}: {ErrorMessage}", user.Id, result.Error.Description);
                    }
                }
                else
                {
                    _logger.LogDebug("Recovery score already generated for user: {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred calculating recovery score for user: {UserId}", user.Id);
            }
        }

        _logger.LogInformation("Completed daily Recovery Calculation Job.");
    }
}
