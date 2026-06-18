using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Auth.Commands.Register;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var isUnique = await _userRepository.IsEmailUniqueAsync(command.Email);
        if (!isUnique)
        {
            return Result.Failure<Guid>(new Error("Auth.DuplicateEmail", "Email address is already in use."));
        }

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = new User(
            command.Email.ToLowerInvariant().Trim(),
            passwordHash,
            command.FirstName,
            command.LastName,
            command.Gender,
            command.DateOfBirth,
            command.Height,
            command.Weight,
            command.FitnessGoal);

        await _userRepository.AddAsync(user);

        return Result.Success(user.Id);
    }
}
