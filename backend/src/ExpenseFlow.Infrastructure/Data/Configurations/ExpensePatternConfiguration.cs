using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExpensePattern entity.
/// </summary>
public class ExpensePatternConfiguration : IEntityTypeConfiguration<ExpensePattern>
{
    public void Configure(EntityTypeBuilder<ExpensePattern> builder)
    {
        builder.ToTable("expense_patterns");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.NormalizedVendor)
            .HasColumnName("normalized_vendor")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(e => e.AverageAmount)
            .HasColumnName("average_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.MinAmount)
            .HasColumnName("min_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.MaxAmount)
            .HasColumnName("max_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.OccurrenceCount)
            .HasColumnName("occurrence_count")
            .HasDefaultValue(0);

        builder.Property(e => e.LastSeenAt)
            .HasColumnName("last_seen_at")
            .IsRequired();

        builder.Property(e => e.DefaultGLCode)
            .HasColumnName("default_gl_code")
            .HasMaxLength(50);

        builder.Property(e => e.DefaultDepartment)
            .HasColumnName("default_department")
            .HasMaxLength(50);

        builder.Property(e => e.ConfirmCount)
            .HasColumnName("confirm_count")
            .HasDefaultValue(0);

        builder.Property(e => e.RejectCount)
            .HasColumnName("reject_count")
            .HasDefaultValue(0);

        builder.Property(e => e.IsSuppressed)
            .HasColumnName("is_suppressed")
            .HasDefaultValue(false);

        builder.Property(e => e.RequiresReceiptMatch)
            .HasColumnName("requires_receipt_match")
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Unique constraint: one pattern per vendor per user
        builder.HasIndex(e => new { e.UserId, e.NormalizedVendor })
            .IsUnique()
            .HasDatabaseName("ix_expense_patterns_user_vendor");

        // Index for efficient user queries
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_expense_patterns_user_id");

        // Relationship
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
