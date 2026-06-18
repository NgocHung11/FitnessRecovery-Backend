using System.Security.Claims;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FluentValidation;

namespace FitnessRecovery.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    double Height,
    double Weight,
    string FitnessGoal);

public static class UpdateProfileEndpoint
{
    public static void MapUpdateProfile(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/auth/profile", async (
            UpdateProfileRequest request,
            UpdateProfileHandler handler,
            IValidator<UpdateProfileCommand> validator,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new UpdateProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.Gender,
                request.DateOfBirth,
                request.Height,
                request.Weight,
                request.FitnessGoal);

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

            return Results.Ok(ApiResponse.CreateSuccess("Profile updated successfully."));
        })
        .WithName("UpdateProfile")
        .RequireAuthorization();
    }
}
