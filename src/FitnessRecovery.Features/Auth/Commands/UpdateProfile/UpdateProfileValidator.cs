using FluentValidation;

namespace FitnessRecovery.Features.Auth.Commands.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly string[] AllowedGenders = ["Male", "Female", "Other"];
    private static readonly string[] AllowedGoals = ["LoseWeight", "MaintainWeight", "BuildMuscle", "ImproveEndurance"];

    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => AllowedGenders.Contains(g)).WithMessage("Gender must be Male, Female, or Other.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth cannot be in the future.")
            .Must(dob => dob <= DateTime.UtcNow.AddYears(-13)).WithMessage("User must be at least 13 years old.");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.");

        RuleFor(x => x.FitnessGoal)
            .NotEmpty().WithMessage("Fitness goal is required.")
            .Must(fg => AllowedGoals.Contains(fg)).WithMessage("Goal must be LoseWeight, MaintainWeight, BuildMuscle, or ImproveEndurance.");
    }
}
