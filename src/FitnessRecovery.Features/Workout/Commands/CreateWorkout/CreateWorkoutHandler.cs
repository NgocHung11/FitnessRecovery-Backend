using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Commands.CreateWorkout;

public class CreateWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;

    public CreateWorkoutHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
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

            return Result.Success(session.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("Workout.Validation", ex.Message));
        }
    }
}
