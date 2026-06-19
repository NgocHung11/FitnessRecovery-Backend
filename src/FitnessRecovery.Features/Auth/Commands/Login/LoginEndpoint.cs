using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FluentValidation;

namespace FitnessRecovery.Features.Auth.Commands.Login;

public static class LoginEndpoint
{
    public static void MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/login", async (
            LoginCommand command,
            LoginHandler handler,
            IValidator<LoginCommand> validator) =>
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

            return Results.Ok(ApiResponse<LoginResponse>.CreateSuccess(result.Value, "Login successful."));
        })
        .WithName("Login")
        .RequireRateLimiting("auth-policy");
    }
}
