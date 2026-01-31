using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Map all properties to snake_case column names
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.BlobUrl).HasColumnName("blob_url").HasMaxLength(500).IsRequired();
        builder.Property(r => r.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(500);
        builder.Property(r => r.OriginalFilename).HasColumnName("original_filename").HasMaxLength(255).IsRequired();
        builder.Property(r => r.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(r => r.FileSize).HasColumnName("file_size");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasDefaultValue(ReceiptStatus.Uploaded);

        builder.Property(r => r.VendorExtracted).HasColumnName("vendor_extracted").HasMaxLength(255);
        builder.Property(r => r.DateExtracted).HasColumnName("date_extracted");
        builder.Property(r => r.AmountExtracted).HasColumnName("amount_extracted").HasPrecision(12, 2);
        builder.Property(r => r.TaxExtracted).HasColumnName("tax_extracted").HasPrecision(12, 2);
        builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("USD");

        // JSONB columns for PostgreSQL
        builder.Property(r => r.LineItems).HasColumnName("line_items").HasColumnType("jsonb");
        builder.Property(r => r.ConfidenceScores).HasColumnName("confidence_scores").HasColumnType("jsonb");

        builder.Property(r => r.ErrorMessage).HasColumnName("error_message");
        builder.Property(r => r.PageCount).HasColumnName("page_count").HasDefaultValue(1);
        builder.Property(r => r.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.ProcessedAt).HasColumnName("processed_at");

        // Sprint 5: Matching fields
        builder.Property(r => r.MatchedTransactionId)
            .HasColumnName("matched_transaction_id")
            .IsRequired(false);

        builder.Property(r => r.MatchStatus)
            .HasColumnName("match_status")
            .HasDefaultValue(MatchStatus.Unmatched)
            .IsRequired();

        // Duplicate detection fields
        builder.Property(r => r.FileHash)
            .HasColumnName("file_hash")
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(r => r.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired(false);

        // Indexes for efficient querying
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.UserId, r.Status });
        builder.HasIndex(r => r.CreatedAt).IsDescending();

        // Sprint 5: Match status index
        builder.HasIndex(r => new { r.UserId, r.MatchStatus })
            .HasDatabaseName("ix_receipts_match_status");

        // Duplicate detection indexes
        builder.HasIndex(r => new { r.UserId, r.FileHash })
            .HasDatabaseName("ix_receipts_file_hash");

        builder.HasIndex(r => new { r.UserId, r.ContentHash })
            .HasDatabaseName("ix_receipts_content_hash");

        // Relationship to User with CASCADE delete
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sprint 5: Relationship to matched Transaction
        builder.HasOne(r => r.MatchedTransaction)
            .WithOne(t => t.MatchedReceipt)
            .HasForeignKey<Receipt>(r => r.MatchedTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Feature 024: Optimistic concurrency using PostgreSQL system column xmin
        builder.Property(r => r.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
