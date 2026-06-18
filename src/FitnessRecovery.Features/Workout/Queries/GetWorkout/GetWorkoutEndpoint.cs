using System.Security.Claims;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Workout.Queries.GetWorkout;

public static class GetWorkoutEndpoint
{
    public static void MapGetWorkout(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workouts/{id:guid}", async (
            Guid id,
            GetWorkoutHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetWorkoutQuery(id, userId);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                if (result.Error.Code == "Error.NotFound")
                {
                    return Results.NotFound(ApiResponse.CreateError("Workout session not found."));
                }
                if (result.Error.Code == "Workout.Unauthorized")
                {
                    return Results.Json(ApiResponse.CreateError(result.Error.Description), statusCode: StatusCodes.Status403Forbidden);
                }
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<WorkoutSessionDto>.CreateSuccess(result.Value));
        })
        .WithName("GetWorkout")
        .RequireAuthorization();
    }
}
// 
