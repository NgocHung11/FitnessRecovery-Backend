using System;

namespace FitnessRecovery.Features.Health.Domain;

public readonly record struct Steps
{
    public int Value { get; }

    public Steps(int value)
    {
        if (value < 0)
        {
            throw new ArgumentException("Steps cannot be negative.", nameof(value));
        }
        Value = value;
    }

    public static implicit operator int(Steps steps) => steps.Value;
    public static implicit operator Steps(int value) => new(value);
}
