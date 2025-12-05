using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DescriptionCache entity.
/// </summary>
public class DescriptionCacheConfiguration : IEntityTypeConfiguration<DescriptionCache>
{
    public void Configure(EntityTypeBuilder<DescriptionCache> builder)
    {
        builder.ToTable("description_cache");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.RawDescriptionHash)
            .HasColumnName("raw_description_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(d => d.RawDescription)
            .HasColumnName("raw_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.NormalizedDescription)
            .HasColumnName("normalized_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.HitCount)
            .HasColumnName("hit_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(d => d.RawDescriptionHash)
            .IsUnique()
            .HasDatabaseName("ix_description_cache_hash");
    }
}
