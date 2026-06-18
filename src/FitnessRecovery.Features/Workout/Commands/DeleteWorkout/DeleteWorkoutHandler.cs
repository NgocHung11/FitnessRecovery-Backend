using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Commands.DeleteWorkout;

public class DeleteWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;

    public DeleteWorkoutHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
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

        return Result.Success();
    }
}
