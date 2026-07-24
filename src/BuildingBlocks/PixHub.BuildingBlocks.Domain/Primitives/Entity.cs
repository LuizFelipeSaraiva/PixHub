namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Base type for entities: identity-based equality over a strongly-typed id.
/// Two entities are equal when they are the same concrete type and share the same id.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // Parameterless ctor for ORM materialization; kept protected so domain code cannot bypass invariants.
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; protected init; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && GetType() == other.GetType() && Id.Equals(other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
