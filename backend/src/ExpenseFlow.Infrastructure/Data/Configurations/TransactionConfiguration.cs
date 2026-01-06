using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Transaction entity.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.ImportId)
            .HasColumnName("import_id")
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(t => t.PostDate)
            .HasColumnName("post_date")
            .IsRequired(false);

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.OriginalDescription)
            .HasColumnName("original_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.DuplicateHash)
            .HasColumnName("duplicate_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.MatchedReceiptId)
            .HasColumnName("matched_receipt_id")
            .IsRequired(false);

        // Sprint 5: Match status
        builder.Property(t => t.MatchStatus)
            .HasColumnName("match_status")
            .HasDefaultValue(MatchStatus.Unmatched)
            .IsRequired();

        // Feature 026: Missing Receipts UI
        builder.Property(t => t.ReceiptUrl)
            .HasColumnName("receipt_url")
            .HasMaxLength(2048)  // URL max length
            .IsRequired(false);

        builder.Property(t => t.ReceiptDismissed)
            .HasColumnName("receipt_dismissed")
            .IsRequired(false);

        // Transaction Grouping: FK to TransactionGroup
        builder.Property(t => t.GroupId)
            .HasColumnName("group_id")
            .IsRequired(false);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Import)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: MatchedReceipt relationship defined in ReceiptConfiguration
        // as a one-to-one relationship via Receipt.MatchedTransactionId

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("ix_transactions_user_id");

        builder.HasIndex(t => t.ImportId)
            .HasDatabaseName("ix_transactions_import_id");

        // Composite index for duplicate detection
        builder.HasIndex(t => new { t.UserId, t.DuplicateHash })
            .HasDatabaseName("ix_transactions_user_duplicate_hash");

        // Composite index for date range queries
        builder.HasIndex(t => new { t.UserId, t.TransactionDate })
            .HasDatabaseName("ix_transactions_user_date");

        // Sprint 5: Match status index
        builder.HasIndex(t => new { t.UserId, t.MatchStatus })
            .HasDatabaseName("ix_transactions_match_status");

        // Feature 026: Missing receipts query index
        builder.HasIndex(t => new { t.UserId, t.MatchedReceiptId, t.ReceiptDismissed })
            .HasDatabaseName("ix_transactions_user_missing_receipt");

        // Transaction Grouping: index for group lookups
        builder.HasIndex(t => t.GroupId)
            .HasDatabaseName("ix_transactions_group_id");
    }
}
