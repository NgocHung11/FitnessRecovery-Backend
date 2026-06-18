using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Auth.Commands.UpdateProfile;

public class UpdateProfileHandler
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return Result.Failure(new Error("Auth.UserNotFound", "User profile not found."));
        }

        user.UpdateProfile(
            command.FirstName,
            command.LastName,
            command.Gender,
            command.DateOfBirth,
            command.Height,
            command.Weight,
            command.FitnessGoal);

        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}
