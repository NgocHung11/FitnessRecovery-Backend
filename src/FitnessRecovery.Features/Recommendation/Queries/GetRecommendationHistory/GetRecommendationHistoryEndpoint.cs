using System;
using System.Security.Claims;
using FitnessRecovery.Features.Recommendation.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Recommendation.Queries.GetRecommendationHistory;

public static class GetRecommendationHistoryEndpoint
{
    public static void MapGetRecommendationHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/recommendations/history", async (
            int? page,
            int? pageSize,
            GetRecommendationHistoryHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var queryPage = page ?? 1;
            var queryPageSize = pageSize ?? 10;

            var query = new GetRecommendationHistoryQuery(userId, queryPage, queryPageSize);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<PagedList<RecommendationDto>>.CreateSuccess(result.Value));
        })
        .WithName("GetRecommendationHistory")
        .RequireAuthorization();
    }
}
