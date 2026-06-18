using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Auth.Queries.GetProfile;

public class GetProfileHandler
{
    private readonly IUserRepository _userRepository;

    public GetProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserProfileResponse>> HandleAsync(GetProfileQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId);
        if (user == null)
        {
            return Result.Failure<UserProfileResponse>(new Error("Auth.UserNotFound", "User profile not found."));
        }

        var response = new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Gender,
            user.DateOfBirth,
            user.Height,
            user.Weight,
            user.FitnessGoal);

        return Result.Success(response);
    }
}
