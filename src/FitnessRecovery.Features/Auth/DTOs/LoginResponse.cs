namespace FitnessRecovery.Features.Auth.DTOs;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
