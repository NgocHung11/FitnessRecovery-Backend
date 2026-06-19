using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitnessRecovery.Infrastructure.BackgroundJobs;

public class DatabaseCleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseCleanupJob> _logger;

    public DatabaseCleanupJob(
        ApplicationDbContext context,
        ILogger<DatabaseCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting monthly Database Cleanup Job...");

        try
        {
            var now = DateTime.UtcNow;

            // Fetch tokens that are expired or revoked
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt <= now || rt.RevokedAt != null)
                .ToListAsync();

            if (oldTokens.Any())
            {
                _logger.LogInformation("Found {Count} expired or revoked refresh tokens. Deleting...", oldTokens.Count);
                _context.RefreshTokens.RemoveRange(oldTokens);
                var deletedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted {Count} refresh tokens.", deletedCount);
            }
            else
            {
                _logger.LogInformation("No expired or revoked refresh tokens found to clean up.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Database Cleanup Job.");
            throw;
        }

        _logger.LogInformation("Completed monthly Database Cleanup Job.");
    }
}
