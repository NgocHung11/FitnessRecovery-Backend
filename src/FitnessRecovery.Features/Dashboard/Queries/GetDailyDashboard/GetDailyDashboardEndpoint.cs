using System;
using System.Security.Claims;
using FitnessRecovery.Features.Dashboard.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Dashboard.Queries.GetDailyDashboard;

public static class GetDailyDashboardEndpoint
{
    public static void MapGetDailyDashboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/dashboard/daily", async (
            GetDailyDashboardHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetDailyDashboardQuery(userId);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<DailyDashboardDto>.CreateSuccess(result.Value));
        })
        .WithName("GetDailyDashboard")
        .RequireAuthorization();
    }
}
