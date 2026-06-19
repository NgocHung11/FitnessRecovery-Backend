using System;
using System.Security.Claims;
using FitnessRecovery.Features.Recommendation.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Recommendation.Queries.GetTodayRecommendation;

public static class GetTodayRecommendationEndpoint
{
    public static void MapGetTodayRecommendation(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/recommendations/today", async (
            GetTodayRecommendationHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetTodayRecommendationQuery(userId);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                if (result.Error.Code == "Recovery.HealthRecordMissing")
                {
                    return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
                }
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<RecommendationDto>.CreateSuccess(result.Value));
        })
        .WithName("GetTodayRecommendation")
        .RequireAuthorization();
    }
}
