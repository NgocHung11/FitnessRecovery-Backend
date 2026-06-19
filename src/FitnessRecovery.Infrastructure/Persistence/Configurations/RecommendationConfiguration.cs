using FitnessRecovery.Features.Recommendation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessRecovery.Infrastructure.Persistence.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("recommendations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.RecoveryAnalysisId)
            .IsRequired();

        builder.Property(r => r.RecommendationType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        // Unique index on RecoveryAnalysisId
        builder.HasIndex(r => r.RecoveryAnalysisId)
            .IsUnique();

        // Foreign key to User
        builder.HasOne<FitnessRecovery.Features.Auth.Domain.User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to RecoveryAnalysis
        builder.HasOne<FitnessRecovery.Features.Recovery.Domain.RecoveryAnalysis>()
            .WithMany()
            .HasForeignKey(r => r.RecoveryAnalysisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
