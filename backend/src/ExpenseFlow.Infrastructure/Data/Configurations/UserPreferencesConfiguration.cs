using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for UserPreferences entity.
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("user_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.Theme)
            .HasColumnName("theme")
            .HasMaxLength(20)
            .HasDefaultValue("system")
            .IsRequired();

        builder.Property(p => p.DefaultDepartmentId)
            .HasColumnName("default_department_id");

        builder.Property(p => p.DefaultProjectId)
            .HasColumnName("default_project_id");

        builder.Property(p => p.EmployeeId)
            .HasColumnName("employee_id")
            .HasMaxLength(50);

        builder.Property(p => p.SupervisorName)
            .HasColumnName("supervisor_name")
            .HasMaxLength(100);

        builder.Property(p => p.DepartmentName)
            .HasColumnName("department_name")
            .HasMaxLength(100);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Unique index on UserId (1:1 relationship)
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("ix_user_preferences_user_id");

        // 1:1 relationship with User
        builder.HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
