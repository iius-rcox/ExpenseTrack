using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for TravelPeriod entity.
/// </summary>
public class TravelPeriodConfiguration : IEntityTypeConfiguration<TravelPeriod>
{
    public void Configure(EntityTypeBuilder<TravelPeriod> builder)
    {
        builder.ToTable("travel_periods");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(t => t.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(t => t.Destination)
            .HasColumnName("destination")
            .HasMaxLength(100);

        builder.Property(t => t.Source)
            .HasColumnName("source")
            .HasDefaultValue(TravelPeriodSource.Manual)
            .IsRequired();

        builder.Property(t => t.SourceReceiptId)
            .HasColumnName("source_receipt_id");

        builder.Property(t => t.RequiresAiReview)
            .HasColumnName("requires_ai_review")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.SourceReceipt)
            .WithMany()
            .HasForeignKey(t => t.SourceReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("ix_travel_periods_user_id");

        builder.HasIndex(t => new { t.UserId, t.StartDate, t.EndDate })
            .HasDatabaseName("ix_travel_periods_user_dates");
    }
}
