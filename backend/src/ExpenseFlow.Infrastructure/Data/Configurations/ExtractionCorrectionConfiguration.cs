using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExtractionCorrection entity.
/// Stores training feedback for AI model improvement.
/// </summary>
public class ExtractionCorrectionConfiguration : IEntityTypeConfiguration<ExtractionCorrection>
{
    public void Configure(EntityTypeBuilder<ExtractionCorrection> builder)
    {
        builder.ToTable("extraction_corrections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OriginalValue)
            .HasColumnName("original_value")
            .HasColumnType("text");

        builder.Property(e => e.CorrectedValue)
            .HasColumnName("corrected_value")
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Indexes for efficient querying
        builder.HasIndex(e => e.ReceiptId)
            .HasDatabaseName("ix_extraction_corrections_receipt_id");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_extraction_corrections_user_id");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_extraction_corrections_created_at");

        builder.HasIndex(e => e.FieldName)
            .HasDatabaseName("ix_extraction_corrections_field_name");

        // Relationships with CASCADE delete
        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
