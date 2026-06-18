using System.Security.Claims;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Workout.Commands.CreateWorkout;

public record CreateWorkoutRequest(
    WorkoutType WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    WorkoutIntensity Intensity,
    string? Notes,
    DateTime WorkoutDate);

public static class CreateWorkoutEndpoint
{
    public static void MapCreateWorkout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/workouts", async (
            CreateWorkoutRequest request,
            CreateWorkoutHandler handler,
            IValidator<CreateWorkoutCommand> validator,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new CreateWorkoutCommand(
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
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<Guid>.CreateSuccess(result.Value, "Workout created successfully."));
        })
        .WithName("CreateWorkout")
        .RequireAuthorization();
    }
}
