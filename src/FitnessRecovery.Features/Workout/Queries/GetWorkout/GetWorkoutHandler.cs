using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Queries.GetWorkout;

public class GetWorkoutHandler
{
    private readonly IWorkoutRepository _workoutRepository;

    public GetWorkoutHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<WorkoutSessionDto>> HandleAsync(GetWorkoutQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _workoutRepository.GetByIdAsync(query.Id);
        if (session == null)
        {
            return Result.Failure<WorkoutSessionDto>(Error.NotFound);
        }

        if (session.UserId != query.UserId)
        {
            return Result.Failure<WorkoutSessionDto>(new Error("Workout.Unauthorized", "You do not have permission to view this workout session."));
        }

        var dto = new WorkoutSessionDto(
            session.Id,
            session.UserId,
            session.WorkoutType.ToString(),
            session.DurationMinutes,
            session.CaloriesBurned,
            session.Intensity.ToString(),
            session.Notes,
            session.WorkoutDate,
            session.CreatedAt,
            session.UpdatedAt);

        return Result.Success(dto);
    }
}
// 
