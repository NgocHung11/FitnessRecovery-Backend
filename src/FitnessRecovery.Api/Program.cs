using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using FitnessRecovery.Api.Middleware;
using FitnessRecovery.Infrastructure.Caching;
using FitnessRecovery.Features.Auth.Commands.Login;
using FitnessRecovery.Features.Auth.Commands.Logout;
using FitnessRecovery.Features.Auth.Commands.Register;
using FitnessRecovery.Features.Auth.Commands.RefreshToken;
using FitnessRecovery.Features.Auth.Commands.UpdateProfile;
using FitnessRecovery.Features.Auth.Contracts;
using FitnessRecovery.Features.Auth.Queries.GetProfile;
using FitnessRecovery.Features.Workout.Commands.CreateWorkout;
using FitnessRecovery.Features.Workout.Commands.UpdateWorkout;
using FitnessRecovery.Features.Workout.Commands.DeleteWorkout;
using FitnessRecovery.Features.Workout.Queries.GetWorkout;
using FitnessRecovery.Features.Workout.Queries.GetWorkoutHistory;
using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Health.Commands.CreateHealthRecord;
using FitnessRecovery.Features.Health.Commands.UpdateHealthRecord;
using FitnessRecovery.Features.Health.Queries.GetHealthRecord;
using FitnessRecovery.Features.Health.Queries.GetHealthRecordHistory;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Recovery.Queries.GetTodayRecovery;
using FitnessRecovery.Features.Recovery.Queries.GetRecoveryHistory;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.Queries.GetTodayRecommendation;
using FitnessRecovery.Features.Recommendation.Queries.GetRecommendationHistory;
using FitnessRecovery.Features.Dashboard.Queries.GetDailyDashboard;
using FitnessRecovery.Features.Dashboard.Queries.GetAnalytics;
using FitnessRecovery.Features.Dashboard.Queries.GetWeeklyReports;
using FitnessRecovery.Features.Dashboard.Contracts;
using FitnessRecovery.Infrastructure.Persistence.Mongo;
using FitnessRecovery.Infrastructure.Authentication;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.Infrastructure.Repositories;
using FitnessRecovery.SharedKernel.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Connection Strings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? "localhost:6379";

// Services Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Swagger with JWT Authorization Support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Fitness Recovery API", Version = "v1" });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 1. Global limit (all endpoints): 60 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    // 2. Auth limit policy: 5 requests per minute per IP
    options.AddPolicy("auth-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

// Security & JWT Configurations
var secretKey = builder.Configuration["Jwt:Key"] ?? "FitnessRecoverySuperSecretKey1234567890!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "FitnessRecovery";
var audience = builder.Configuration["Jwt:Audience"] ?? "FitnessRecoveryApi";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// DI Container Registrations
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenCacheService, TokenCacheService>();
builder.Services.AddScoped<IDashboardCacheService, DashboardCacheService>();
builder.Services.AddScoped<IRecoveryCacheService, RecoveryCacheService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IHealthRecordRepository, HealthRecordRepository>();
builder.Services.AddScoped<IRecoveryRepository, RecoveryRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IHealthRecordMongoRepository, HealthRecordMongoRepository>();
builder.Services.AddScoped<IRecoveryAnalysisMongoRepository, RecoveryAnalysisMongoRepository>();
builder.Services.AddScoped<IWeeklyReportMongoRepository, WeeklyReportMongoRepository>();
builder.Services.AddScoped<MongoMigrationService>();

// Auto-register Validators
builder.Services.AddValidatorsFromAssembly(typeof(FitnessRecovery.Features.Auth.Domain.User).Assembly);

// Register Handlers
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<GetProfileHandler>();
builder.Services.AddScoped<UpdateProfileHandler>();
builder.Services.AddScoped<CreateWorkoutHandler>();
builder.Services.AddScoped<UpdateWorkoutHandler>();
builder.Services.AddScoped<DeleteWorkoutHandler>();
builder.Services.AddScoped<GetWorkoutHandler>();
builder.Services.AddScoped<GetWorkoutHistoryHandler>();
builder.Services.AddScoped<CreateHealthRecordHandler>();
builder.Services.AddScoped<UpdateHealthRecordHandler>();
builder.Services.AddScoped<GetHealthRecordHandler>();
builder.Services.AddScoped<GetHealthRecordHistoryHandler>();
builder.Services.AddScoped<GetTodayRecoveryHandler>();
builder.Services.AddScoped<GetRecoveryHistoryHandler>();
builder.Services.AddScoped<GetTodayRecommendationHandler>();
builder.Services.AddScoped<GetRecommendationHistoryHandler>();
builder.Services.AddScoped<GetDailyDashboardHandler>();
builder.Services.AddScoped<GetAnalyticsHandler>();
builder.Services.AddScoped<GetWeeklyReportsHandler>();

var app = builder.Build();

// Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseRateLimiter();

// Token Blacklist Middleware checks Redis before auth authorization rules block access
app.UseMiddleware<TokenBlacklistMiddleware>();

app.UseAuthentication();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Baseline Health Endpoint
app.MapGet("/api/v1/health", () => 
{
    return Results.Ok(ApiResponse.CreateSuccess("Fitness Recovery API is healthy and operational."));
})
.WithName("GetHealth");

// Map Authentication Endpoints Slices
app.MapRegisterUser();
app.MapLogin();
app.MapRefreshToken();
app.MapLogout();
app.MapGetProfile();
app.MapUpdateProfile();

// Map Workout Endpoints Slices
app.MapCreateWorkout();
app.MapUpdateWorkout();
app.MapDeleteWorkout();
app.MapGetWorkout();
app.MapGetWorkoutHistory();

// Map Health Endpoints Slices
app.MapCreateHealthRecord();
app.MapUpdateHealthRecord();
app.MapGetHealthRecord();
app.MapGetHealthRecordHistory();

// Map Recovery Endpoints Slices
app.MapGetTodayRecovery();
app.MapGetRecoveryHistory();

// Map Recommendation Endpoints Slices
app.MapGetTodayRecommendation();
app.MapGetRecommendationHistory();

// Map Dashboard Endpoints Slices
app.MapGetDailyDashboard();
app.MapGetAnalytics();
app.MapGetWeeklyReports();

// Automatic DB Migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    // Initialize MongoDB indexes and migration sync
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await MongoIndexConfig.CreateIndexesAsync(mongoContext);

    var mongoMigration = scope.ServiceProvider.GetRequiredService<MongoMigrationService>();
    await mongoMigration.MigrateAsync();
}

app.Run();
