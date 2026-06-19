using System;
using System.Security.Claims;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;

public static class GetTodayRecoveryEndpoint
{
    public static void MapGetTodayRecovery(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/recovery/today", async (
            GetTodayRecoveryHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetTodayRecoveryQuery(userId);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                if (result.Error.Code == "Recovery.HealthRecordMissing")
                {
                    return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
                }
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<RecoveryAnalysisDto>.CreateSuccess(result.Value));
        })
        .WithName("GetTodayRecovery")
        .RequireAuthorization();
    }
}
