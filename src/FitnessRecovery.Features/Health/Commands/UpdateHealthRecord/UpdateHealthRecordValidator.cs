using System;
using FitnessRecovery.Features.Health.Domain;
using FluentValidation;

namespace FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;

public class UpdateHealthRecordValidator : AbstractValidator<UpdateHealthRecordCommand>
{
    public UpdateHealthRecordValidator()
    {
        RuleFor(x => x.RecordDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Record date cannot be in the future.");

        RuleFor(x => x.SleepHours)
            .InclusiveBetween(0.0, 24.0)
            .WithMessage("Sleep hours must be between 0 and 24.");

        RuleFor(x => x.SleepQuality)
            .IsEnumName(typeof(SleepQuality), caseSensitive: false)
            .WithMessage("Invalid sleep quality value.");

        RuleFor(x => x.RestingHeartRate)
            .InclusiveBetween(20, 250)
            .WithMessage("Resting heart rate must be between 20 and 250 bpm.");

        RuleFor(x => x.AverageHeartRate)
            .InclusiveBetween(20, 250)
            .WithMessage("Average heart rate must be between 20 and 250 bpm.");

        RuleFor(x => x.Steps)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Steps cannot be negative.");

        RuleFor(x => x.Weight)
            .GreaterThan(0.0)
            .WithMessage("Weight must be greater than zero.");

        RuleFor(x => x.CaloriesBurned)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Calories burned cannot be negative.");
    }
}
