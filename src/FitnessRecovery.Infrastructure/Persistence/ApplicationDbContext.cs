using FitnessRecovery.Features.Auth.Domain;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Features.Recovery.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitnessRecovery.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<HealthRecord> HealthRecords => Set<HealthRecord>();

    public DbSet<RecoveryAnalysis> RecoveryAnalyses => Set<RecoveryAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
