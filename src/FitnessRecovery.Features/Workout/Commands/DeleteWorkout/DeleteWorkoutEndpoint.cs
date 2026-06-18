using System.Security.Claims;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Workout.Commands.DeleteWorkout;

public static class DeleteWorkoutEndpoint
{
    public static void MapDeleteWorkout(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/workouts/{id:guid}", async (
            Guid id,
            DeleteWorkoutHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new DeleteWorkoutCommand(id, userId);
            var result = await handler.HandleAsync(command);
            
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

            return Results.Ok(ApiResponse.CreateSuccess("Workout deleted successfully."));
        })
        .WithName("DeleteWorkout")
        .RequireAuthorization();
    }
}
