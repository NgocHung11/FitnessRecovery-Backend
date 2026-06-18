using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Queries.GetWorkoutHistory;

public class GetWorkoutHistoryHandler
{
    private readonly IWorkoutRepository _workoutRepository;

    public GetWorkoutHistoryHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<PagedList<WorkoutSessionDto>>> HandleAsync(GetWorkoutHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result.Failure<PagedList<WorkoutSessionDto>>(new Error("Workout.History", "Page index must be greater than zero."));
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            return Result.Failure<PagedList<WorkoutSessionDto>>(new Error("Workout.History", "Page size must be between 1 and 100."));
        }

        var pagedSessions = await _workoutRepository.GetPagedByUserIdAsync(query.UserId, query.Page, query.PageSize);

        var dtos = pagedSessions.Items.Select(session => new WorkoutSessionDto(
            session.Id,
            session.UserId,
            session.WorkoutType.ToString(),
            session.DurationMinutes,
            session.CaloriesBurned,
            session.Intensity.ToString(),
            session.Notes,
            session.WorkoutDate,
            session.CreatedAt,
            session.UpdatedAt)).ToList();

        var result = new PagedList<WorkoutSessionDto>(dtos, pagedSessions.Page, pagedSessions.PageSize, pagedSessions.TotalItems);

        return Result.Success(result);
    }
}
