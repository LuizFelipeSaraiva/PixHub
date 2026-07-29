namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Tipo base dos value objects do DDD: a igualdade é estrutural (pelos componentes devolvidos por
/// <see cref="GetEqualityComponents"/>) e as instâncias são tratadas como imutáveis.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Conjunto ordenado de valores que define a igualdade deste value object.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null && GetType() == other.GetType() && ValuesAreEqual(other);

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    private bool ValuesAreEqual(ValueObject other) =>
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
