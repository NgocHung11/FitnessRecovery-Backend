using System.Security.Claims;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Auth.Queries.GetProfile;

public static class GetProfileEndpoint
{
    public static void MapGetProfile(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/auth/profile", async (
            GetProfileHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetProfileQuery(userId);
            var result = await handler.HandleAsync(query);
            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<UserProfileResponse>.CreateSuccess(result.Value));
        })
        .WithName("GetProfile")
        .RequireAuthorization();
    }
}
