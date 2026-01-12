using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for TransactionGroup entity.
/// </summary>
public class TransactionGroupConfiguration : IEntityTypeConfiguration<TransactionGroup>
{
    public void Configure(EntityTypeBuilder<TransactionGroup> builder)
    {
        builder.ToTable("transaction_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(g => g.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(g => g.DisplayDate)
            .HasColumnName("display_date")
            .IsRequired();

        builder.Property(g => g.IsDateOverridden)
            .HasColumnName("is_date_overridden")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(g => g.CombinedAmount)
            .HasColumnName("combined_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(g => g.TransactionCount)
            .HasColumnName("transaction_count")
            .IsRequired();

        builder.Property(g => g.MatchedReceiptId)
            .HasColumnName("matched_receipt_id")
            .IsRequired(false);

        builder.Property(g => g.MatchStatus)
            .HasColumnName("match_status")
            .HasDefaultValue(MatchStatus.Unmatched)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(g => g.MerchantName)
            .HasColumnName("merchant_name")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(g => g.IsReimbursable)
            .HasColumnName("is_reimbursable")
            .IsRequired(false);

        builder.Property(g => g.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(g => g.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Relationships
        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.MatchedReceipt)
            .WithMany()
            .HasForeignKey(g => g.MatchedReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        // One-to-many: Group has many Transactions
        builder.HasMany(g => g.Transactions)
            .WithOne(t => t.Group)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(g => g.UserId)
            .HasDatabaseName("ix_transaction_groups_user_id");

        builder.HasIndex(g => new { g.UserId, g.MatchStatus })
            .HasDatabaseName("ix_transaction_groups_user_match_status");

        builder.HasIndex(g => new { g.UserId, g.DisplayDate })
            .HasDatabaseName("ix_transaction_groups_user_date");
    }
}
