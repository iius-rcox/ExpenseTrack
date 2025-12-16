using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for TierUsageLog entity.
/// </summary>
public class TierUsageLogConfiguration : IEntityTypeConfiguration<TierUsageLog>
{
    public void Configure(EntityTypeBuilder<TierUsageLog> builder)
    {
        builder.ToTable("tier_usage_logs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.TransactionId)
            .HasColumnName("transaction_id");

        builder.Property(t => t.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.TierUsed)
            .HasColumnName("tier_used")
            .IsRequired();

        builder.Property(t => t.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(3, 2);

        builder.Property(t => t.ResponseTimeMs)
            .HasColumnName("response_time_ms");

        builder.Property(t => t.CacheHit)
            .HasColumnName("cache_hit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Navigation properties
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Transaction)
            .WithMany()
            .HasForeignKey(t => t.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for analytics queries
        builder.HasIndex(t => new { t.UserId, t.CreatedAt })
            .HasDatabaseName("ix_tier_usage_logs_user_date");

        builder.HasIndex(t => new { t.OperationType, t.TierUsed, t.CreatedAt })
            .HasDatabaseName("ix_tier_usage_logs_type_tier");

        // Check constraint for tier values
        builder.HasCheckConstraint("ck_tier_usage_logs_tier", "tier_used BETWEEN 1 AND 4");
    }
}
