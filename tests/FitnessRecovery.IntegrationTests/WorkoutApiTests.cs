using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Commands.Login;
using FitnessRecovery.Features.Auth.Commands.Register;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.Features.Workout.Commands.CreateWorkout;
using FitnessRecovery.Features.Workout.Commands.UpdateWorkout;
using FitnessRecovery.Features.Workout.DTOs;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using Xunit;

namespace FitnessRecovery.IntegrationTests;

public class WorkoutApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WorkoutApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WorkoutFlow_ShouldCreateUpdateRetrieveListAndDeleteWorkoutsSuccessfully()
    {
        // 1. Register & Login to get token (using valid goal "BuildMuscle")
        var email = $"user_{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        var registerCommand = new RegisterUserCommand(
            email, password, "Workout", "User", "Male", DateTime.UtcNow.AddYears(-30), 180.0, 80.0, "BuildMuscle"
        );
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = loginResult!.Data!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create Workout Session
        var createRequest = new CreateWorkoutRequest(
            WorkoutType.Running,
            30,
            300,
            WorkoutIntensity.Moderate,
            "Morning run",
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/workouts", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        var workoutId = createResult.Data;
        workoutId.Should().NotBeEmpty();

        // 3. Get Workout Details
        var getResponse = await _client.GetAsync($"/api/v1/workouts/{workoutId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<WorkoutSessionDto>>();
        getResult.Should().NotBeNull();
        getResult!.Success.Should().BeTrue();
        getResult.Data!.WorkoutType.Should().Be("Running");
        getResult.Data.DurationMinutes.Should().Be(30);

        // 4. Update Workout Session
        var updateRequest = new UpdateWorkoutRequest(
            WorkoutType.Cycling,
            45,
            450,
            WorkoutIntensity.High,
            "Intense cycling session",
            DateTime.UtcNow
        );

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/workouts/{workoutId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify updated values
        var verifyResponse = await _client.GetAsync($"/api/v1/workouts/{workoutId}");
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<WorkoutSessionDto>>();
        verifyResult!.Data!.WorkoutType.Should().Be("Cycling");
        verifyResult.Data.DurationMinutes.Should().Be(45);
        verifyResult.Data.Notes.Should().Be("Intense cycling session");

        // 5. Get History List
        var historyResponse = await _client.GetAsync("/api/v1/workouts?page=1&pageSize=10");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<PagedList<WorkoutSessionDto>>>();
        historyResult.Should().NotBeNull();
        historyResult!.Success.Should().BeTrue();
        historyResult.Data!.Items.Should().Contain(w => w.Id == workoutId);

        // 6. Delete Workout
        var deleteResponse = await _client.DeleteAsync($"/api/v1/workouts/{workoutId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it returns 404/NotFound after deletion
        var checkResponse = await _client.GetAsync($"/api/v1/workouts/{workoutId}");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
