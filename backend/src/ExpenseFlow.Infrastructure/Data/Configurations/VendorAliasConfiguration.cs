using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for VendorAlias entity.
/// </summary>
public class VendorAliasConfiguration : IEntityTypeConfiguration<VendorAlias>
{
    public void Configure(EntityTypeBuilder<VendorAlias> builder)
    {
        builder.ToTable("vendor_aliases");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(v => v.CanonicalName)
            .HasColumnName("canonical_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(v => v.AliasPattern)
            .HasColumnName("alias_pattern")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(v => v.DefaultGLCode)
            .HasColumnName("default_gl_code")
            .HasMaxLength(10);

        builder.Property(v => v.DefaultDepartment)
            .HasColumnName("default_department")
            .HasMaxLength(20);

        builder.Property(v => v.GLConfirmCount)
            .HasColumnName("gl_confirm_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.DeptConfirmCount)
            .HasColumnName("dept_confirm_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.MatchCount)
            .HasColumnName("match_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.LastMatchedAt)
            .HasColumnName("last_matched_at");

        builder.Property(v => v.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(3, 2)
            .HasDefaultValue(1.00m)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(v => v.AliasPattern)
            .HasDatabaseName("ix_vendor_aliases_pattern");

        builder.HasIndex(v => v.CanonicalName)
            .HasDatabaseName("ix_vendor_aliases_canonical");
    }
}
