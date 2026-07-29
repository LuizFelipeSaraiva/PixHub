namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Tipo base das entidades: igualdade por identidade sobre um id fortemente tipado.
/// Duas entidades são iguais quando são do mesmo tipo concreto e compartilham o mesmo id.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // Construtor sem parâmetros para materialização pelo ORM; fica protected para que o código de
    // domínio não consiga burlar as invariantes.
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
