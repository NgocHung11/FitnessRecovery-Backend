using FitnessRecovery.Features.Auth.Domain;

namespace FitnessRecovery.Features.Auth.Contracts;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    
    Task<User?> GetByEmailAsync(string email);
    
    Task<User?> GetByUserWithRefreshTokensAsync(Guid userId);
    
    Task<User?> GetByRefreshTokenAsync(string token);
    
    Task AddAsync(User user);
    
    Task UpdateAsync(User user);
    
    Task<bool> IsEmailUniqueAsync(string email);
    
    Task<List<User>> GetAllUsersAsync();
}
