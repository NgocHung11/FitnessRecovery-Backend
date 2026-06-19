using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FitnessRecovery.Features.Auth.Commands.Login;
using FitnessRecovery.Features.Auth.Commands.Register;
using FitnessRecovery.Features.Auth.DTOs;
using FitnessRecovery.Features.Health.Commands.CreateHealthRecord;
using FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;
using FitnessRecovery.Features.Health.DTOs;
using FitnessRecovery.SharedKernel.Models;
using FluentAssertions;
using Xunit;

namespace FitnessRecovery.IntegrationTests;

public class HealthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthFlow_ShouldCreateUpdateRetrieveAndListHealthRecordsSuccessfully()
    {
        // 1. Register & Login to get token (using valid goal "BuildMuscle")
        var email = $"user_{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        var registerCommand = new RegisterUserCommand(
            email, password, "Health", "User", "Female", DateTime.UtcNow.AddYears(-28), 165.0, 58.0, "BuildMuscle"
        );
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = loginResult!.Data!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create Health Record
        var recordDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var dateString = recordDate.ToString("yyyy-MM-dd");

        var createRequest = new CreateHealthRecordRequest(
            recordDate,
            7.5,
            "Good",
            60,
            72,
            8500,
            58.5,
            250
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/health-records", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        var recordId = createResult.Data;
        recordId.Should().NotBeEmpty();

        // 3. Get Health Record Details
        var getResponse = await _client.GetAsync($"/api/v1/health-records/{dateString}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<HealthRecordDto>>();
        getResult.Should().NotBeNull();
        getResult!.Success.Should().BeTrue();
        getResult.Data!.SleepHours.Should().Be(7.5);
        getResult.Data.SleepQuality.Should().Be("Good");

        // 4. Update Health Record
        var updateRequest = new UpdateHealthRecordRequest(
            8.0,
            "Excellent",
            58,
            70,
            12000,
            58.2,
            300
        );

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/health-records/{dateString}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify updated values
        var verifyResponse = await _client.GetAsync($"/api/v1/health-records/{dateString}");
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<HealthRecordDto>>();
        verifyResult!.Data!.SleepHours.Should().Be(8.0);
        verifyResult.Data.SleepQuality.Should().Be("Excellent");
        verifyResult.Data.Steps.Should().Be(12000);

        // 5. Get History List
        var historyResponse = await _client.GetAsync("/api/v1/health-records?page=1&pageSize=10");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<PagedList<HealthRecordDto>>>();
        historyResult.Should().NotBeNull();
        historyResult!.Success.Should().BeTrue();
        historyResult.Data!.Items.Should().Contain(h => h.Id == recordId);
    }
}
