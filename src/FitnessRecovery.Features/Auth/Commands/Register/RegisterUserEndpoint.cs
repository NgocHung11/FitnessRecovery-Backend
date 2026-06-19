using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FluentValidation;

namespace FitnessRecovery.Features.Auth.Commands.Register;

public static class RegisterUserEndpoint
{
    public static void MapRegisterUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/register", async (
            RegisterUserCommand command,
            RegisterUserHandler handler,
            IValidator<RegisterUserCommand> validator) =>
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

            return Results.Ok(ApiResponse<Guid>.CreateSuccess(result.Value, "User registered successfully."));
        })
        .WithName("RegisterUser")
        .RequireRateLimiting("auth-policy");
    }
}
