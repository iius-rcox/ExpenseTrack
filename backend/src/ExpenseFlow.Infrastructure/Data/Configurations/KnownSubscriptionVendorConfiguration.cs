using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for KnownSubscriptionVendor entity.
/// </summary>
public class KnownSubscriptionVendorConfiguration : IEntityTypeConfiguration<KnownSubscriptionVendor>
{
    public void Configure(EntityTypeBuilder<KnownSubscriptionVendor> builder)
    {
        builder.ToTable("known_subscription_vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(v => v.VendorPattern)
            .HasColumnName("vendor_pattern")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Category)
            .HasColumnName("category")
            .HasMaxLength(50);

        builder.Property(v => v.TypicalAmount)
            .HasColumnName("typical_amount")
            .HasPrecision(18, 2);

        builder.Property(v => v.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(v => v.VendorPattern)
            .IsUnique()
            .HasDatabaseName("ix_known_subscription_vendors_pattern");

        builder.HasIndex(v => v.IsActive)
            .HasDatabaseName("ix_known_subscription_vendors_active");
    }
}
