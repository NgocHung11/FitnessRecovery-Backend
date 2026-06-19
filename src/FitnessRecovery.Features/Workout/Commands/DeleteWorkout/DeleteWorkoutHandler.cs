using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Commands.DeleteWorkout;

public class DeleteWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IRecoveryCacheService _recoveryCacheService;
    private readonly IDashboardCacheService _dashboardCacheService;

    public DeleteWorkoutHandler(
        IWorkoutRepository workoutRepository,
        IRecoveryCacheService recoveryCacheService,
        IDashboardCacheService dashboardCacheService)
    {
        _workoutRepository = workoutRepository;
        _recoveryCacheService = recoveryCacheService;
        _dashboardCacheService = dashboardCacheService;
    }

    public async Task<Result> HandleAsync(DeleteWorkoutCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _workoutRepository.GetByIdAsync(command.Id);
        if (session == null)
        {
            return Result.Failure(Error.NotFound);
        }

        if (session.UserId != command.UserId)
        {
            return Result.Failure(new Error("Workout.Unauthorized", "You do not have permission to delete this workout session."));
        }

        await _workoutRepository.DeleteAsync(session);

        // Invalidate caches
        await _recoveryCacheService.InvalidateTodayRecoveryAsync(command.UserId);
        await _dashboardCacheService.InvalidateDailyDashboardAsync(command.UserId);
        await _dashboardCacheService.InvalidateWeeklyReportsAsync(command.UserId);

        return Result.Success();
    }
}
