using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExpenseLine entity.
/// </summary>
public class ExpenseLineConfiguration : IEntityTypeConfiguration<ExpenseLine>
{
    public void Configure(EntityTypeBuilder<ExpenseLine> builder)
    {
        builder.ToTable("expense_lines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ReportId)
            .HasColumnName("report_id")
            .IsRequired();

        builder.Property(e => e.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired(false);

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired(false);

        builder.Property(e => e.LineOrder)
            .HasColumnName("line_order")
            .IsRequired();

        builder.Property(e => e.ExpenseDate)
            .HasColumnName("expense_date")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.OriginalDescription)
            .HasColumnName("original_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.NormalizedDescription)
            .HasColumnName("normalized_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.VendorName)
            .HasColumnName("vendor_name")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(e => e.GLCode)
            .HasColumnName("gl_code")
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(e => e.GLCodeSuggested)
            .HasColumnName("gl_code_suggested")
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(e => e.GLCodeTier)
            .HasColumnName("gl_code_tier")
            .IsRequired(false);

        builder.Property(e => e.GLCodeSource)
            .HasColumnName("gl_code_source")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(e => e.DepartmentCode)
            .HasColumnName("department_code")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(e => e.DepartmentSuggested)
            .HasColumnName("department_suggested")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(e => e.DepartmentTier)
            .HasColumnName("department_tier")
            .IsRequired(false);

        builder.Property(e => e.DepartmentSource)
            .HasColumnName("department_source")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(e => e.HasReceipt)
            .HasColumnName("has_receipt")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.MissingReceiptJustification)
            .HasColumnName("missing_receipt_justification")
            .HasConversion<short?>()
            .IsRequired(false);

        builder.Property(e => e.JustificationNote)
            .HasColumnName("justification_note")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.IsUserEdited)
            .HasColumnName("is_user_edited")
            .HasDefaultValue(false)
            .IsRequired();

        // Feature 023: Expense Prediction auto-suggestion tracking
        builder.Property(e => e.IsAutoSuggested)
            .HasColumnName("is_auto_suggested")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.PredictionId)
            .HasColumnName("prediction_id")
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Split allocation fields
        builder.Property(e => e.ParentLineId)
            .HasColumnName("parent_line_id")
            .IsRequired(false);

        builder.Property(e => e.SplitPercentage)
            .HasColumnName("split_percentage")
            .HasPrecision(5, 2)
            .IsRequired(false);

        builder.Property(e => e.IsSplitParent)
            .HasColumnName("is_split_parent")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.IsSplitChild)
            .HasColumnName("is_split_child")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.AllocationOrder)
            .HasColumnName("allocation_order")
            .IsRequired(false);

        // Relationships
        builder.HasOne(e => e.Report)
            .WithMany(r => r.Lines)
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing relationship for split allocations
        builder.HasOne(e => e.ParentLine)
            .WithMany(e => e.ChildAllocations)
            .HasForeignKey(e => e.ParentLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.ReportId, e.LineOrder })
            .HasDatabaseName("ix_expense_lines_report_order");

        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("ix_expense_lines_transaction");

        // Partial index on parent_line_id for efficient child lookups (reviewer recommendation)
        builder.HasIndex(e => e.ParentLineId)
            .HasDatabaseName("ix_expense_lines_parent")
            .HasFilter("parent_line_id IS NOT NULL");

        // Check constraint: at least one of ReceiptId or TransactionId must be set
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_expense_line_has_source",
            "receipt_id IS NOT NULL OR transaction_id IS NOT NULL"));

        // Check constraint: tier values must be 1, 2, or 3 if set
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_gl_tier_valid",
            "gl_code_tier IS NULL OR gl_code_tier IN (1, 2, 3)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_dept_tier_valid",
            "department_tier IS NULL OR department_tier IN (1, 2, 3)"));

        // Check constraint: split consistency (reviewer recommendation)
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_split_consistency",
            "(is_split_child = false AND parent_line_id IS NULL) OR (is_split_child = true AND parent_line_id IS NOT NULL)"));
    }
}
