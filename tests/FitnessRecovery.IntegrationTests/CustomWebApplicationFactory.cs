using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NSubstitute;
using Hangfire;
using FitnessRecovery.Infrastructure.Persistence.Mongo;
using FitnessRecovery.Infrastructure.Persistence.Mongo.Documents;

namespace FitnessRecovery.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public MongoDbContext MockMongoDbContext { get; } = Substitute.For<MongoDbContext>();

    private const string TestJwtKey = "FitnessRecoverySuperSecretKeyForIntegrationTesting1234567890!";

    public CustomWebApplicationFactory()
    {
        // Mock Hangfire's global static JobStorage to prevent dashboard routing errors
        try
        {
            JobStorage.Current = Substitute.For<JobStorage>();
        }
        catch
        {
            // Ignored if already set
        }

        // Configure MongoDB Mock collections to prevent Index and Migration initialization errors on startup
        var mockHealthCollection = Substitute.For<IMongoCollection<HealthRecordDocument>>();
        var mockHealthIndexes = Substitute.For<IMongoIndexManager<HealthRecordDocument>>();
        mockHealthCollection.Indexes.Returns(mockHealthIndexes);
        MockMongoDbContext.GetCollection<HealthRecordDocument>("health_records").Returns(mockHealthCollection);

        var mockRecoveryCollection = Substitute.For<IMongoCollection<RecoveryAnalysisDocument>>();
        var mockRecoveryIndexes = Substitute.For<IMongoIndexManager<RecoveryAnalysisDocument>>();
        mockRecoveryCollection.Indexes.Returns(mockRecoveryIndexes);
        MockMongoDbContext.GetCollection<RecoveryAnalysisDocument>("recovery_analyses").Returns(mockRecoveryCollection);

        var mockReportCollection = Substitute.For<IMongoCollection<WeeklyReportDocument>>();
        var mockReportIndexes = Substitute.For<IMongoIndexManager<WeeklyReportDocument>>();
        mockReportCollection.Indexes.Returns(mockReportIndexes);
        MockMongoDbContext.GetCollection<WeeklyReportDocument>("weekly_reports").Returns(mockReportCollection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing so Program.cs uses InMemory database
        builder.UseEnvironment("Testing");

        // Override configuration values for JWT key to ensure it is long enough for HS256 (>= 256 bits)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "FitnessRecovery",
                ["Jwt:Audience"] = "FitnessRecoveryApi"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1. Replace MongoDbContext with mocked version
            var mongoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MongoDbContext));
            if (mongoDescriptor != null)
            {
                services.Remove(mongoDescriptor);
            }
            services.AddSingleton(MockMongoDbContext);

            // 2. Replace IDistributedCache with in-memory distributed cache
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
            if (cacheDescriptor != null)
            {
                services.Remove(cacheDescriptor);
            }
            services.AddDistributedMemoryCache();

            // 3. Mock Hangfire dependencies and remove background hosted servers to avoid real PostgreSQL connections
            var recurringJobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRecurringJobManager));
            if (recurringJobDescriptor != null)
            {
                services.Remove(recurringJobDescriptor);
            }
            services.AddSingleton(Substitute.For<IRecurringJobManager>());

            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(JobStorage));
            if (storageDescriptor != null)
            {
                services.Remove(storageDescriptor);
            }
            services.AddSingleton(JobStorage.Current);

            // Remove any Hangfire background server hosted service
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                if (service.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.OrdinalIgnoreCase) == true)
                {
                    services.Remove(service);
                }
            }

            // 4. Overwrite token validation parameters in JwtBearerOptions so it matches the test key
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(TestJwtKey));
                options.TokenValidationParameters.ValidIssuer = "FitnessRecovery";
                options.TokenValidationParameters.ValidAudience = "FitnessRecoveryApi";
            });
        });
    }
}
