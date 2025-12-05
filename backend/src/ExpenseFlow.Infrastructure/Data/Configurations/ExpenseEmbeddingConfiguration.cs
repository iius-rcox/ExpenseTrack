using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExpenseEmbedding entity.
/// </summary>
public class ExpenseEmbeddingConfiguration : IEntityTypeConfiguration<ExpenseEmbedding>
{
    public void Configure(EntityTypeBuilder<ExpenseEmbedding> builder)
    {
        builder.ToTable("expense_embeddings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ExpenseLineId)
            .HasColumnName("expense_line_id");

        builder.Property(e => e.VendorNormalized)
            .HasColumnName("vendor_normalized")
            .HasMaxLength(255);

        builder.Property(e => e.DescriptionText)
            .HasColumnName("description_text")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.GLCode)
            .HasColumnName("gl_code")
            .HasMaxLength(10);

        builder.Property(e => e.Department)
            .HasColumnName("department")
            .HasMaxLength(20);

        builder.Property(e => e.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(1536)")
            .IsRequired();

        builder.Property(e => e.Verified)
            .HasColumnName("verified")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // IVFFlat index for vector similarity search
        // Note: This index is created via raw SQL in migration because EF Core
        // doesn't support IVFFlat index creation directly
        builder.HasIndex(e => e.Embedding)
            .HasDatabaseName("ix_expense_embeddings_vector");
    }
}
