using System;
using System.Security.Claims;
using FitnessRecovery.Features.Recovery.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Recovery.Queries.GetRecoveryHistory;

public static class GetRecoveryHistoryEndpoint
{
    public static void MapGetRecoveryHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/recovery/history", async (
            int? page,
            int? pageSize,
            GetRecoveryHistoryHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var queryPage = page ?? 1;
            var queryPageSize = pageSize ?? 10;

            var query = new GetRecoveryHistoryQuery(userId, queryPage, queryPageSize);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<PagedList<RecoveryAnalysisDto>>.CreateSuccess(result.Value));
        })
        .WithName("GetRecoveryHistory")
        .RequireAuthorization();
    }
}
