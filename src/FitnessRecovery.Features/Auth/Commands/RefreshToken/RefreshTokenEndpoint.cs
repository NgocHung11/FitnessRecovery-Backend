using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FluentValidation;

namespace FitnessRecovery.Features.Auth.Commands.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static void MapRefreshToken(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/refresh-token", async (
            RefreshTokenCommand command,
            RefreshTokenHandler handler,
            IValidator<RefreshTokenCommand> validator) =>
        {
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

            return Results.Ok(ApiResponse<LoginResponse>.CreateSuccess(result.Value, "Token refreshed successfully."));
        })
        .WithName("RefreshToken")
        .RequireRateLimiting("auth-policy");
    }
}
