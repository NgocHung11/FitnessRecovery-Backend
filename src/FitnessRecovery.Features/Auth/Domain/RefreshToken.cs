using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Auth.Domain;

public class RefreshToken : Entity
{
    private RefreshToken() { } // EF Core constructor

    public RefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt == null && !IsExpired;

    public void Revoke()
    {
        if (RevokedAt == null)
        {
            RevokedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
