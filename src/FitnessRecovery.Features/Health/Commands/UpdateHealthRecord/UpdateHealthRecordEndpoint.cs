using System;
using System.Linq;
using System.Security.Claims;
using FitnessRecovery.SharedKernel.Models;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;

public record UpdateHealthRecordRequest(
    double SleepHours,
    string SleepQuality,
    int RestingHeartRate,
    int AverageHeartRate,
    int Steps,
    double Weight,
    int CaloriesBurned);

public static class UpdateHealthRecordEndpoint
{
    public static void MapUpdateHealthRecord(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/health-records/{date}", async (
            string date,
            UpdateHealthRecordRequest request,
            UpdateHealthRecordHandler handler,
            IValidator<UpdateHealthRecordCommand> validator,
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

            var command = new UpdateHealthRecordCommand(
                userId,
                recordDate,
                request.SleepHours,
                request.SleepQuality,
                request.RestingHeartRate,
                request.AverageHeartRate,
                request.Steps,
                request.Weight,
                request.CaloriesBurned);

            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(ApiResponse.CreateError("Validation failed.", errors));
            }

            var result = await handler.HandleAsync(command);
            if (result.IsFailure)
            {
                if (result.Error.Code == "Error.NotFound")
                {
                    return Results.NotFound(ApiResponse.CreateError(result.Error.Description));
                }
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse.CreateSuccess("Health record updated successfully."));
        })
        .WithName("UpdateHealthRecord")
        .RequireAuthorization();
    }
}
