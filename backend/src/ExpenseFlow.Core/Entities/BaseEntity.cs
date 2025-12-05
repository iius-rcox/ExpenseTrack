using ExpenseFlow.Core.Interfaces;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
