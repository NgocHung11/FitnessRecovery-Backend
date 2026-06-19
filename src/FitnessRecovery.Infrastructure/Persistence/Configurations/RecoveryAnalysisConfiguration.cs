using FitnessRecovery.Features.Recovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessRecovery.Infrastructure.Persistence.Configurations;

public class RecoveryAnalysisConfiguration : IEntityTypeConfiguration<RecoveryAnalysis>
{
    public void Configure(EntityTypeBuilder<RecoveryAnalysis> builder)
    {
        builder.ToTable("recovery_analyses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.AnalysisDate)
            .IsRequired();

        builder.Property(r => r.RecoveryScore)
            .HasConversion(
                vo => vo.Value,
                v => new RecoveryScore(v))
            .IsRequired();

        builder.Property(r => r.RecoveryStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.SleepScore)
            .IsRequired();

        builder.Property(r => r.HeartRateScore)
            .IsRequired();

        builder.Property(r => r.WorkoutLoadScore)
            .IsRequired();

        builder.Property(r => r.ActivityScore)
            .IsRequired();

        builder.Property(r => r.GeneratedAt)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        // Unique constraint for user and date
        builder.HasIndex(r => new { r.UserId, r.AnalysisDate })
            .IsUnique();

        // Foreign key to User entity (represented in Auth context)
        builder.HasOne<FitnessRecovery.Features.Auth.Domain.User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
