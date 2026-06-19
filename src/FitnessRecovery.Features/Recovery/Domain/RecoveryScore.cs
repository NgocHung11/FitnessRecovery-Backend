using System;

namespace FitnessRecovery.Features.Recovery.Domain;

public readonly record struct RecoveryScore
{
    public int Value { get; }

    public RecoveryScore(int value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentException("Recovery score must be between 0 and 100.", nameof(value));
        }
        Value = value;
    }

    public static implicit operator int(RecoveryScore score) => score.Value;
    public static implicit operator RecoveryScore(int value) => new(value);
}
