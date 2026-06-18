using FitnessRecovery.SharedKernel.Domain;

namespace FitnessRecovery.Features.Auth.Domain;

public class User : Entity
{
    private readonly List<RefreshToken> _refreshTokens = new();

    private User() { } // EF Core constructor

    public User(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string gender,
        DateTime dateOfBirth,
        double height,
        double weight,
        string fitnessGoal)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        FitnessGoal = fitnessGoal;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Gender { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public double Height { get; private set; }
    public double Weight { get; private set; }
    public string FitnessGoal { get; private set; } = string.Empty;

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public void UpdateProfile(
        string firstName,
        string lastName,
        string gender,
        DateTime dateOfBirth,
        double height,
        double weight,
        string fitnessGoal)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        FitnessGoal = fitnessGoal;
        UpdateTimestamp();
    }

    public void AddRefreshToken(string token, DateTime expiresAt)
    {
        _refreshTokens.Add(new RefreshToken(Id, token, expiresAt));
    }

    public void RevokeRefreshToken(string token)
    {
        var rt = _refreshTokens.FirstOrDefault(x => x.Token == token);
        rt?.Revoke();
    }
}
