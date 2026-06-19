using System;
using System.Security.Claims;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Dashboard.Queries.GetAnalytics;

public static class GetAnalyticsEndpoint
{
    public static void MapGetAnalytics(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/dashboard/analytics", async (
            GetAnalyticsHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetAnalyticsQuery(userId);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<AnalyticsDto>.CreateSuccess(result.Value));
        })
        .WithName("GetAnalytics")
        .RequireAuthorization();
    }
}
