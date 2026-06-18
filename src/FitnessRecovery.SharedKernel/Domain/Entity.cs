namespace FitnessRecovery.SharedKernel.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; protected set; }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
