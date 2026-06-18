namespace FitnessRecovery.Features.Auth.DTOs;

public record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    double Height,
    double Weight,
    string FitnessGoal);
