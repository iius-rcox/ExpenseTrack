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

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id");

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

        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at");

        // Navigation properties
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        // NOTE: Vector index (ix_expense_embeddings_vector) is created via raw SQL migration
        // because EF Core's HasIndex creates B-tree indexes which cannot handle large vectors.
        // See migration script for HNSW index creation:
        //   CREATE INDEX ix_expense_embeddings_vector ON expense_embeddings
        //   USING hnsw (embedding vector_cosine_ops) WITH (m = 16, ef_construction = 64);

        builder.HasIndex(e => new { e.Verified, e.UserId })
            .HasDatabaseName("ix_expense_embeddings_verified_user");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("ix_expense_embeddings_expires")
            .HasFilter("expires_at IS NOT NULL");
    }
}
