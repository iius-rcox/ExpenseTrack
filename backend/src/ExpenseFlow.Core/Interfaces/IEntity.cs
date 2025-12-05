namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Base interface for all entities with a unique identifier.
/// </summary>
public interface IEntity
{
    Guid Id { get; set; }
}

/// <summary>
/// Interface for entities that track creation time.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
}
