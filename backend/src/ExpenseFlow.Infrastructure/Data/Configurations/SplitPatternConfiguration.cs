using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for SplitPattern entity.
/// </summary>
public class SplitPatternConfiguration : IEntityTypeConfiguration<SplitPattern>
{
    public void Configure(EntityTypeBuilder<SplitPattern> builder)
    {
        builder.ToTable("split_patterns");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.VendorAliasId)
            .HasColumnName("vendor_alias_id");

        builder.Property(s => s.SplitConfig)
            .HasColumnName("split_config")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.UsageCount)
            .HasColumnName("usage_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.VendorAlias)
            .WithMany(v => v.SplitPatterns)
            .HasForeignKey(s => s.VendorAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_split_patterns_user_id");

        builder.HasIndex(s => new { s.UserId, s.VendorAliasId })
            .HasDatabaseName("ix_split_patterns_user_vendor");
    }
}
