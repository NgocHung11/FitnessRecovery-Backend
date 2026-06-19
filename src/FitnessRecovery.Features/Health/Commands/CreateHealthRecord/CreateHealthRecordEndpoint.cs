using System;
using System.Linq;
using System.Security.Claims;
using FitnessRecovery.SharedKernel.Models;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FitnessRecovery.Features.Health.Commands.CreateHealthRecord;

public record CreateHealthRecordRequest(
    DateOnly RecordDate,
    double SleepHours,
    string SleepQuality,
    int RestingHeartRate,
    int AverageHeartRate,
    int Steps,
    double Weight,
    int CaloriesBurned);

public static class CreateHealthRecordEndpoint
{
    public static void MapCreateHealthRecord(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/health-records", async (
            CreateHealthRecordRequest request,
            CreateHealthRecordHandler handler,
            IValidator<CreateHealthRecordCommand> validator,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new CreateHealthRecordCommand(
                userId,
                request.RecordDate,
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
                return Results.BadRequest(ApiResponse.CreateError(result.Error.Description));
            }

            return Results.Ok(ApiResponse<Guid>.CreateSuccess(result.Value, "Health record created successfully."));
        })
        .WithName("CreateHealthRecord")
        .RequireAuthorization();
    }
}
