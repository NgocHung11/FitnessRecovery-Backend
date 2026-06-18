using System.Security.Claims;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Workout.Commands.UpdateWorkout;

public record UpdateWorkoutRequest(
    WorkoutType WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    WorkoutIntensity Intensity,
    string? Notes,
    DateTime WorkoutDate);

public static class UpdateWorkoutEndpoint
{
    public static void MapUpdateWorkout(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/workouts/{id:guid}", async (
            Guid id,
            UpdateWorkoutRequest request,
            UpdateWorkoutHandler handler,
            IValidator<UpdateWorkoutCommand> validator,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new UpdateWorkoutCommand(
                id,
                userId,
                request.WorkoutType,
                request.DurationMinutes,
                request.CaloriesBurned,
                request.Intensity,
                request.Notes,
                request.WorkoutDate);

            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(ApiResponse.CreateError("Validation failed.", errors));
            }

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

            return Results.Ok(ApiResponse.CreateSuccess("Workout updated successfully."));
        })
        .WithName("UpdateWorkout")
        .RequireAuthorization();
    }
}
