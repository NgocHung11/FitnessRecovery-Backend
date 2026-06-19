using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Commands.UpdateWorkout;

public class UpdateWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IRecoveryCacheService _recoveryCacheService;
    private readonly IDashboardCacheService _dashboardCacheService;

    public UpdateWorkoutHandler(
        IWorkoutRepository workoutRepository,
        IRecoveryCacheService recoveryCacheService,
        IDashboardCacheService dashboardCacheService)
    {
        _workoutRepository = workoutRepository;
        _recoveryCacheService = recoveryCacheService;
        _dashboardCacheService = dashboardCacheService;
    }

    public async Task<Result> HandleAsync(UpdateWorkoutCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _workoutRepository.GetByIdAsync(command.Id);
        if (session == null)
        {
            return Result.Failure(Error.NotFound);
        }

        if (session.UserId != command.UserId)
        {
            return Result.Failure(new Error("Workout.Unauthorized", "You do not have permission to update this workout session."));
        }

        try
        {
            session.Update(
                command.WorkoutType,
                command.DurationMinutes,
                command.CaloriesBurned,
                command.Intensity,
                command.Notes,
                command.WorkoutDate);

            await _workoutRepository.UpdateAsync(session);

            // Invalidate caches
            await _recoveryCacheService.InvalidateTodayRecoveryAsync(command.UserId);
            await _dashboardCacheService.InvalidateDailyDashboardAsync(command.UserId);
            await _dashboardCacheService.InvalidateWeeklyReportsAsync(command.UserId);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("Workout.Validation", ex.Message));
        }
    }
}
// 
