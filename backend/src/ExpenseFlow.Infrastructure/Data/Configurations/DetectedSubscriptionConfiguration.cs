using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DetectedSubscription entity.
/// </summary>
public class DetectedSubscriptionConfiguration : IEntityTypeConfiguration<DetectedSubscription>
{
    public void Configure(EntityTypeBuilder<DetectedSubscription> builder)
    {
        builder.ToTable("detected_subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.VendorAliasId)
            .HasColumnName("vendor_alias_id");

        builder.Property(s => s.VendorName)
            .HasColumnName("vendor_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.AverageAmount)
            .HasColumnName("average_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.OccurrenceMonths)
            .HasColumnName("occurrence_months")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(s => s.LastSeenDate)
            .HasColumnName("last_seen_date")
            .IsRequired();

        builder.Property(s => s.ExpectedNextDate)
            .HasColumnName("expected_next_date");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasDefaultValue(SubscriptionStatus.Active)
            .IsRequired();

        builder.Property(s => s.DetectionSource)
            .HasColumnName("detection_source")
            .HasDefaultValue(DetectionSource.PatternMatch)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.VendorAlias)
            .WithMany()
            .HasForeignKey(s => s.VendorAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_detected_subscriptions_user_id");

        builder.HasIndex(s => new { s.UserId, s.Status })
            .HasDatabaseName("ix_detected_subscriptions_user_status");

        builder.HasIndex(s => s.VendorAliasId)
            .HasDatabaseName("ix_detected_subscriptions_vendor_alias_id");
    }
}
