using System.Security.Claims;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Workout.Queries.GetWorkoutHistory;

public static class GetWorkoutHistoryEndpoint
{
    public static void MapGetWorkoutHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workouts", async (
            int? page,
            int? pageSize,
            GetWorkoutHistoryHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var queryPage = page ?? 1;
            var queryPageSize = pageSize ?? 10;

            var query = new GetWorkoutHistoryQuery(userId, queryPage, queryPageSize);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<PagedList<WorkoutSessionDto>>.CreateSuccess(result.Value));
        })
        .WithName("GetWorkoutHistory")
        .RequireAuthorization();
    }
}
