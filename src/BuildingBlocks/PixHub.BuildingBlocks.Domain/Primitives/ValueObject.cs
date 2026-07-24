namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Base type for DDD value objects: equality is structural (by the components returned from
/// <see cref="GetEqualityComponents"/>), and instances are treated as immutable.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The ordered set of values that define equality for this value object.</summary>
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
