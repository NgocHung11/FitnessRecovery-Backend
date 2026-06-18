namespace FitnessRecovery.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    double Height,
    double Weight,
    string FitnessGoal);
