using System;
using System.Security.Claims;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecordHistory;

public static class GetHealthRecordHistoryEndpoint
{
    public static void MapGetHealthRecordHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/health-records", async (
            int? page,
            int? pageSize,
            GetHealthRecordHistoryHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var queryPage = page ?? 1;
            var queryPageSize = pageSize ?? 10;

            var query = new GetHealthRecordHistoryQuery(userId, queryPage, queryPageSize);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<PagedList<HealthRecordDto>>.CreateSuccess(result.Value));
        })
        .WithName("GetHealthRecordHistory")
        .RequireAuthorization();
    }
}
