namespace PixHub.Accounts.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for an account. A record struct gives value equality for free and
/// prevents accidentally passing a raw <see cref="Guid"/> where an account id is expected.
/// Uses a v7 (time-ordered) GUID so database indexes stay locality-friendly.
/// </summary>
public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.CreateVersion7());

    public static AccountId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
