using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Commands.CreateWorkout;

public class CreateWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IRecoveryCacheService _recoveryCacheService;
    private readonly IDashboardCacheService _dashboardCacheService;

    public CreateWorkoutHandler(
        IWorkoutRepository workoutRepository,
        IRecoveryCacheService recoveryCacheService,
        IDashboardCacheService dashboardCacheService)
    {
        _workoutRepository = workoutRepository;
        _recoveryCacheService = recoveryCacheService;
        _dashboardCacheService = dashboardCacheService;
    }

    public async Task<Result<Guid>> HandleAsync(CreateWorkoutCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = new WorkoutSession(
                command.UserId,
                command.WorkoutType,
                command.DurationMinutes,
                command.CaloriesBurned,
                command.Intensity,
                command.Notes,
                command.WorkoutDate);

            await _workoutRepository.AddAsync(session);

            // Invalidate caches
            await _recoveryCacheService.InvalidateTodayRecoveryAsync(command.UserId);
            await _dashboardCacheService.InvalidateDailyDashboardAsync(command.UserId);
            await _dashboardCacheService.InvalidateWeeklyReportsAsync(command.UserId);

            return Result.Success(session.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("Workout.Validation", ex.Message));
        }
    }
}
