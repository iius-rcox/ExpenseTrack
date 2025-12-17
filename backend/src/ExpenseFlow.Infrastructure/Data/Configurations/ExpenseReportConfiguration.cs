using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExpenseReport entity.
/// </summary>
public class ExpenseReportConfiguration : IEntityTypeConfiguration<ExpenseReport>
{
    public void Configure(EntityTypeBuilder<ExpenseReport> builder)
    {
        builder.ToTable("expense_reports");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Period)
            .HasColumnName("period")
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasDefaultValue(ReportStatus.Draft)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(e => e.LineCount)
            .HasColumnName("line_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.MissingReceiptCount)
            .HasColumnName("missing_receipt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.Tier1HitCount)
            .HasColumnName("tier1_hit_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.Tier2HitCount)
            .HasColumnName("tier2_hit_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.Tier3HitCount)
            .HasColumnName("tier3_hit_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // PostgreSQL xmin column for optimistic locking
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lines)
            .WithOne(l => l.Report)
            .HasForeignKey(l => l.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // Unique index on (UserId, Period) for active (non-deleted) reports
        builder.HasIndex(e => new { e.UserId, e.Period })
            .HasDatabaseName("ix_expense_reports_user_period")
            .IsUnique()
            .HasFilter("NOT is_deleted");

        // Index for listing reports by user, ordered by created date
        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .HasDatabaseName("ix_expense_reports_user_created")
            .IsDescending(false, true);

        // Check constraint: period format YYYY-MM
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_period_format",
            "period ~ '^\\d{4}-(0[1-9]|1[0-2])$'"));

        // Check constraint: status must be valid enum value
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_report_status_valid",
            "status >= 0 AND status <= 3"));
    }
}
