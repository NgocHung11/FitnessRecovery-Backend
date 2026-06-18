using FluentValidation;

namespace FitnessRecovery.Features.Workout.Commands.UpdateWorkout;

public class UpdateWorkoutValidator : AbstractValidator<UpdateWorkoutCommand>
{
    public UpdateWorkoutValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Workout ID is required.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");

        RuleFor(x => x.CaloriesBurned)
            .GreaterThanOrEqualTo(0).WithMessage("Calories burned cannot be negative.");

        RuleFor(x => x.WorkoutDate)
            .LessThanOrEqualTo(_ => DateTime.UtcNow).WithMessage("Workout date cannot be in the future.");

        RuleFor(x => x.WorkoutType)
            .IsInEnum().WithMessage("Invalid workout type.");

        RuleFor(x => x.Intensity)
            .IsInEnum().WithMessage("Invalid workout intensity.");
            
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
