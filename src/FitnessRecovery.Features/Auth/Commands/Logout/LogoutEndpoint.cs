using System.Security.Claims;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Auth.Commands.Logout;

public static class LogoutEndpoint
{
    public static void MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/logout", async (
            LogoutCommand command,
            LogoutHandler handler,
            HttpContext httpContext) =>
        {
            string? jti = null;
            TimeSpan? remainingTime = null;

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                jti = httpContext.User.FindFirst("jti")?.Value;
                var expClaim = httpContext.User.FindFirst("exp")?.Value;
                if (long.TryParse(expClaim, out var expUnix))
                {
                    var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    remainingTime = expirationTime - DateTime.UtcNow;
                }
            }

            var result = await handler.HandleAsync(command, jti, remainingTime);
            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse.CreateSuccess("Logged out successfully."));
        })
        .WithName("Logout");
    }
}
