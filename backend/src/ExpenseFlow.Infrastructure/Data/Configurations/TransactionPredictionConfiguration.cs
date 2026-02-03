using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for TransactionPrediction entity.
/// </summary>
public class TransactionPredictionConfiguration : IEntityTypeConfiguration<TransactionPrediction>
{
    public void Configure(EntityTypeBuilder<TransactionPrediction> builder)
    {
        builder.ToTable("transaction_predictions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.PatternId)
            .HasColumnName("pattern_id")
            .IsRequired(false); // Nullable for manual overrides

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(e => e.ConfidenceLevel)
            .HasColumnName("confidence_level")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PredictionStatus.Pending)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(e => e.IsManualOverride)
            .HasColumnName("is_manual_override")
            .HasDefaultValue(false);

        builder.Property(e => e.IsPersonalPrediction)
            .HasColumnName("is_personal_prediction")
            .HasDefaultValue(false);

        // Unique: one prediction per transaction
        builder.HasIndex(e => e.TransactionId)
            .IsUnique()
            .HasDatabaseName("ix_transaction_predictions_transaction");

        // Index for user queries
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_transaction_predictions_user_id");

        // Index for pending predictions
        builder.HasIndex(e => new { e.UserId, e.Status })
            .HasDatabaseName("ix_transaction_predictions_user_status");

        // Relationships
        builder.HasOne(e => e.Pattern)
            .WithMany(p => p.Predictions)
            .HasForeignKey(e => e.PatternId)
            .IsRequired(false) // Nullable for manual overrides
            .OnDelete(DeleteBehavior.SetNull); // If pattern deleted, keep prediction as manual

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
