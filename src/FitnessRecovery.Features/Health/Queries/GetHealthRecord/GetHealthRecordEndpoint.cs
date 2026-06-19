using System;
using System.Security.Claims;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecord;

public static class GetHealthRecordEndpoint
{
    public static void MapGetHealthRecord(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/health-records/{date}", async (
            string date,
            GetHealthRecordHandler handler,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var recordDate))
            {
                return Results.BadRequest(ApiResponse.CreateError("Invalid date format. Use yyyy-MM-dd."));
            }

            var query = new GetHealthRecordQuery(userId, recordDate);
            var result = await handler.HandleAsync(query);

            if (result.IsFailure)
            {
                if (result.Error.Code == "Error.NotFound")
                {
                    return Results.NotFound(ApiResponse.CreateError(result.Error.Description));
                }
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<HealthRecordDto>.CreateSuccess(result.Value));
        })
        .WithName("GetHealthRecord")
        .RequireAuthorization();
    }
}
