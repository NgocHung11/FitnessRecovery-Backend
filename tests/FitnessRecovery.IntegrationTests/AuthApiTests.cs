using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Commands.Login;
using FitnessRecovery.Features.Auth.Commands.Register;
using FitnessRecovery.Features.Auth.Commands.UpdateProfile;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using Xunit;

namespace FitnessRecovery.IntegrationTests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthFlow_ShouldRegisterLoginGetAndUpdateProfileSuccessfully()
    {
        var email = $"user_{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        // 1. Register User
        var registerCommand = new RegisterUserCommand(
            email,
            password,
            "John",
            "Doe",
            "Male",
            DateTime.UtcNow.AddYears(-25),
            180.0,
            75.0,
            "BuildMuscle"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);
        if (registerResponse.StatusCode != HttpStatusCode.OK)
        {
            var content = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed with status {registerResponse.StatusCode}. Response: {content}");
        }
        
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        registerResult.Should().NotBeNull();
        registerResult!.Success.Should().BeTrue();
        registerResult.Data.Should().NotBeEmpty();

        // 2. Login User
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {loginResponse.StatusCode}. Response: {content}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var token = loginResult.Data.AccessToken;

        // 3. Get Profile
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/profile");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var profileResponse = await _client.SendAsync(requestMessage);
        if (profileResponse.StatusCode != HttpStatusCode.OK)
        {
            var content = await profileResponse.Content.ReadAsStringAsync();
            throw new Exception($"Profile request failed with status {profileResponse.StatusCode}. Response: {content}");
        }

        var profileResult = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
        profileResult.Should().NotBeNull();
        profileResult!.Success.Should().BeTrue();
        profileResult.Data!.Email.Should().Be(email);
        profileResult.Data.FirstName.Should().Be("John");

        // 4. Update Profile
        var updateRequest = new UpdateProfileRequest("Jane", "Smith", "Female", DateTime.UtcNow.AddYears(-26), 170.0, 65.0, "LoseWeight");
        var updateRequestMessage = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        updateRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateResponse = await _client.SendAsync(updateRequestMessage);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResponse>();
        updateResult.Should().NotBeNull();
        updateResult!.Success.Should().BeTrue();
    }
}
