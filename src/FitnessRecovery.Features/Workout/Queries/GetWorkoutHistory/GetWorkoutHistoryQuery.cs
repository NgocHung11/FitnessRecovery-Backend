namespace FitnessRecovery.Features.Workout.Queries.GetWorkoutHistory;

public record GetWorkoutHistoryQuery(Guid UserId, int Page, int PageSize);
