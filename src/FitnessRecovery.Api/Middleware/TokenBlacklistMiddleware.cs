using System.IdentityModel.Tokens.Jwt;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Api.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenCacheService tokenCacheService)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenString = authHeader["Bearer ".Length..].Trim();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(tokenString))
                {
                    var jwtToken = handler.ReadJwtToken(tokenString);
                    var jti = jwtToken.Id; // Extracts the Jti claim
                    
                    if (!string.IsNullOrEmpty(jti) && await tokenCacheService.IsAccessTokenBlacklistedAsync(jti))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var response = ApiResponse.CreateError("Token has been revoked. Please log in again.");
                        await context.Response.WriteAsJsonAsync(response);
                        return;
                    }
                }
            }
            catch
            {
                // Let the JwtBearer authentication middleware handle token parsing errors
            }
        }

        await _next(context);
    }
}
