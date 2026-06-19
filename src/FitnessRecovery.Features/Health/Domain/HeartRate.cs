using System;

namespace FitnessRecovery.Features.Health.Domain;

public readonly record struct HeartRate
{
    public int Value { get; }

    public HeartRate(int value)
    {
        if (value < 20 || value > 250)
        {
            throw new ArgumentException("Heart rate must be between 20 and 250 bpm.", nameof(value));
        }
        Value = value;
    }

    public static implicit operator int(HeartRate heartRate) => heartRate.Value;
    public static implicit operator HeartRate(int value) => new(value);
}
