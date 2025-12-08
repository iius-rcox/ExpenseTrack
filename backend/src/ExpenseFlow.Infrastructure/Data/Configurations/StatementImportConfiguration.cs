using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for StatementImport entity.
/// </summary>
public class StatementImportConfiguration : IEntityTypeConfiguration<StatementImport>
{
    public void Configure(EntityTypeBuilder<StatementImport> builder)
    {
        builder.ToTable("statement_imports");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(i => i.FingerprintId)
            .HasColumnName("fingerprint_id")
            .IsRequired(false);

        builder.Property(i => i.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        builder.Property(i => i.TierUsed)
            .HasColumnName("tier_used")
            .IsRequired();

        builder.Property(i => i.TransactionCount)
            .HasColumnName("transaction_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.SkippedCount)
            .HasColumnName("skipped_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.DuplicateCount)
            .HasColumnName("duplicate_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Fingerprint)
            .WithMany()
            .HasForeignKey(i => i.FingerprintId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(i => i.UserId)
            .HasDatabaseName("ix_statement_imports_user_id");

        // Index for recent imports list (descending)
        builder.HasIndex(i => i.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_statement_imports_created_at");
    }
}
