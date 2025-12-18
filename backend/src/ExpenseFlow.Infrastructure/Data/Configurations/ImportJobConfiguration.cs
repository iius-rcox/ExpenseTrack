using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ImportJob entity.
/// </summary>
public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("import_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.SourceFileName)
            .HasColumnName("source_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.BlobUrl)
            .HasColumnName("blob_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.TotalRecords)
            .HasColumnName("total_records")
            .HasDefaultValue(0);

        builder.Property(x => x.ProcessedRecords)
            .HasColumnName("processed_records")
            .HasDefaultValue(0);

        builder.Property(x => x.CachedDescriptions)
            .HasColumnName("cached_descriptions")
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAliases)
            .HasColumnName("created_aliases")
            .HasDefaultValue(0);

        builder.Property(x => x.GeneratedEmbeddings)
            .HasColumnName("generated_embeddings")
            .HasDefaultValue(0);

        builder.Property(x => x.SkippedRecords)
            .HasColumnName("skipped_records")
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorLog)
            .HasColumnName("error_log")
            .HasMaxLength(10000);

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("ix_import_jobs_user_id_status");

        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("ix_import_jobs_started_at")
            .IsDescending();

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
