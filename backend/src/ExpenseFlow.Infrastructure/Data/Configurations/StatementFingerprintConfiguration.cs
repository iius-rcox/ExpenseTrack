using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for StatementFingerprint entity.
/// </summary>
public class StatementFingerprintConfiguration : IEntityTypeConfiguration<StatementFingerprint>
{
    public void Configure(EntityTypeBuilder<StatementFingerprint> builder)
    {
        builder.ToTable("statement_fingerprints");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.SourceName)
            .HasColumnName("source_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.HeaderHash)
            .HasColumnName("header_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.ColumnMapping)
            .HasColumnName("column_mapping")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.DateFormat)
            .HasColumnName("date_format")
            .HasMaxLength(50);

        builder.Property(s => s.AmountSign)
            .HasColumnName("amount_sign")
            .HasMaxLength(20)
            .HasDefaultValue("negative_charges")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany(u => u.StatementFingerprints)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(s => new { s.UserId, s.HeaderHash })
            .IsUnique()
            .HasDatabaseName("ix_statement_fingerprints_user_hash");
    }
}
