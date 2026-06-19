using System;

namespace FitnessRecovery.Features.Health.Domain;

public readonly record struct SleepHours
{
    public double Value { get; }

    public SleepHours(double value)
    {
        if (value < 0.0 || value > 24.0)
        {
            throw new ArgumentException("Sleep hours must be between 0 and 24.", nameof(value));
        }
        Value = value;
    }

    public static implicit operator double(SleepHours sleepHours) => sleepHours.Value;
    public static implicit operator SleepHours(double value) => new(value);
}
