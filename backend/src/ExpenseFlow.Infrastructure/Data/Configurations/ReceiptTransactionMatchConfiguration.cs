using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ReceiptTransactionMatch entity.
/// </summary>
public class ReceiptTransactionMatchConfiguration : IEntityTypeConfiguration<ReceiptTransactionMatch>
{
    public void Configure(EntityTypeBuilder<ReceiptTransactionMatch> builder)
    {
        builder.ToTable("receipt_transaction_matches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired();

        builder.Property(m => m.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasDefaultValue(MatchProposalStatus.Proposed)
            .IsRequired();

        builder.Property(m => m.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(m => m.AmountScore)
            .HasColumnName("amount_score")
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(m => m.DateScore)
            .HasColumnName("date_score")
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(m => m.VendorScore)
            .HasColumnName("vendor_score")
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(m => m.MatchReason)
            .HasColumnName("match_reason")
            .HasMaxLength(500);

        builder.Property(m => m.MatchedVendorAliasId)
            .HasColumnName("matched_vendor_alias_id")
            .IsRequired(false);

        builder.Property(m => m.IsManualMatch)
            .HasColumnName("is_manual_match")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(m => m.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .IsRequired(false);

        builder.Property(m => m.ConfirmedByUserId)
            .HasColumnName("confirmed_by_user_id")
            .IsRequired(false);

        // PostgreSQL xmin column for optimistic locking
        builder.Property(m => m.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // Relationships
        builder.HasOne(m => m.Receipt)
            .WithMany()
            .HasForeignKey(m => m.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Transaction)
            .WithMany()
            .HasForeignKey(m => m.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.MatchedVendorAlias)
            .WithMany()
            .HasForeignKey(m => m.MatchedVendorAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(m => m.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(m => new { m.UserId, m.Status })
            .HasDatabaseName("ix_rtm_user_status");

        builder.HasIndex(m => m.ReceiptId)
            .HasDatabaseName("ix_rtm_receipt");

        builder.HasIndex(m => m.TransactionId)
            .HasDatabaseName("ix_rtm_transaction");

        // Partial unique index: one receipt can only have one confirmed match
        builder.HasIndex(m => m.ReceiptId)
            .HasDatabaseName("ix_rtm_receipt_confirmed")
            .IsUnique()
            .HasFilter("status = 1");

        // Partial unique index: one transaction can only have one confirmed match
        builder.HasIndex(m => m.TransactionId)
            .HasDatabaseName("ix_rtm_transaction_confirmed")
            .IsUnique()
            .HasFilter("status = 1");

        // Check constraint: confidence must be in valid range
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_confidence_range",
            "confidence_score >= 0 AND confidence_score <= 100"));

        // Check constraint: status must be valid enum value
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_status_valid",
            "status IN (0, 1, 2)"));
    }
}
