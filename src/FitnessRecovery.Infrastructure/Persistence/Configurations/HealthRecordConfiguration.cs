using FitnessRecovery.Features.Health.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessRecovery.Infrastructure.Persistence.Configurations;

public class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord>
{
    public void Configure(EntityTypeBuilder<HealthRecord> builder)
    {
        builder.ToTable("health_records");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.UserId)
            .IsRequired();

        builder.Property(h => h.RecordDate)
            .IsRequired();

        builder.Property(h => h.SleepHours)
            .HasConversion(
                vo => vo.Value,
                v => new SleepHours(v))
            .IsRequired();

        builder.Property(h => h.SleepQuality)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.RestingHeartRate)
            .HasConversion(
                vo => vo.Value,
                v => new HeartRate(v))
            .IsRequired();

        builder.Property(h => h.AverageHeartRate)
            .HasConversion(
                vo => vo.Value,
                v => new HeartRate(v))
            .IsRequired();

        builder.Property(h => h.Steps)
            .HasConversion(
                vo => vo.Value,
                v => new Steps(v))
            .IsRequired();

        builder.Property(h => h.Weight)
            .IsRequired();

        builder.Property(h => h.CaloriesBurned)
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.UpdatedAt);

        // Ensure unique record per user per day
        builder.HasIndex(h => new { h.UserId, h.RecordDate })
            .IsUnique();

        // Foreign Key relation to User (represented in Auth context)
        builder.HasOne<FitnessRecovery.Features.Auth.Domain.User>()
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
