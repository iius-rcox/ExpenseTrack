using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PredictionFeedback entity.
/// </summary>
public class PredictionFeedbackConfiguration : IEntityTypeConfiguration<PredictionFeedback>
{
    public void Configure(EntityTypeBuilder<PredictionFeedback> builder)
    {
        builder.ToTable("prediction_feedback");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.PredictionId)
            .HasColumnName("prediction_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.FeedbackType)
            .HasColumnName("feedback_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Index for analytics
        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .HasDatabaseName("ix_prediction_feedback_user_created");

        // Relationships
        builder.HasOne(e => e.Prediction)
            .WithMany()
            .HasForeignKey(e => e.PredictionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
