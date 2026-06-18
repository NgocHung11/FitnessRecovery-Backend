namespace FitnessRecovery.SharedKernel.Models;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    
    public static readonly Error NullValue = new("Error.NullValue", "The specified value is null.");
    
    public static readonly Error ValidationError = new("Error.Validation", "Validation failed.");
    
    public static readonly Error NotFound = new("Error.NotFound", "The specified resource was not found.");

    public static implicit operator string(Error error) => error.Code;
}
