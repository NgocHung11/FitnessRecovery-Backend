using FitnessRecovery.SharedKernel.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace FitnessRecovery.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        // Standard HTTP status code, error message and list of error details
        var (statusCode, message, errors) = exception switch
        {
            // Future validation exceptions (e.g. FluentValidation.ValidationException) can be added here
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                new List<string> { exception.Message }
            )
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse.CreateError(message, errors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
