using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for RecurringAllowance entity.
/// </summary>
public class RecurringAllowanceConfiguration : IEntityTypeConfiguration<RecurringAllowance>
{
    public void Configure(EntityTypeBuilder<RecurringAllowance> builder)
    {
        builder.ToTable("recurring_allowances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.VendorName)
            .HasColumnName("vendor_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Frequency)
            .HasColumnName("frequency")
            .HasDefaultValue(AllowanceFrequency.Monthly)
            .IsRequired();

        builder.Property(a => a.GLCode)
            .HasColumnName("gl_code")
            .HasMaxLength(20);

        builder.Property(a => a.GLName)
            .HasColumnName("gl_name")
            .HasMaxLength(100);

        builder.Property(a => a.DepartmentCode)
            .HasColumnName("department_code")
            .HasMaxLength(20);

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        // PostgreSQL xmin column for optimistic concurrency control
        builder.Property(a => a.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("ix_recurring_allowances_user_id");

        builder.HasIndex(a => new { a.UserId, a.IsActive })
            .HasDatabaseName("ix_recurring_allowances_user_active");
    }
}
