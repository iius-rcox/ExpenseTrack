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
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.BlobUrl).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ThumbnailUrl).HasMaxLength(500);
        builder.Property(r => r.OriginalFilename).HasMaxLength(255).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100).IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasDefaultValue(ReceiptStatus.Uploaded);

        builder.Property(r => r.VendorExtracted).HasMaxLength(255);
        builder.Property(r => r.AmountExtracted).HasPrecision(12, 2);
        builder.Property(r => r.TaxExtracted).HasPrecision(12, 2);
        builder.Property(r => r.Currency).HasMaxLength(3).HasDefaultValue("USD");

        // JSONB columns for PostgreSQL
        builder.Property(r => r.LineItems).HasColumnType("jsonb");
        builder.Property(r => r.ConfidenceScores).HasColumnType("jsonb");

        builder.Property(r => r.PageCount).HasDefaultValue(1);
        builder.Property(r => r.RetryCount).HasDefaultValue(0);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");

        // Indexes for efficient querying
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.UserId, r.Status });
        builder.HasIndex(r => r.CreatedAt).IsDescending();

        // Relationship to User with CASCADE delete
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
