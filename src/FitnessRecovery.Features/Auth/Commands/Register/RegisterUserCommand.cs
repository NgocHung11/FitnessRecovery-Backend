namespace FitnessRecovery.Features.Auth.Commands.Register;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    double Height,
    double Weight,
    string FitnessGoal);
