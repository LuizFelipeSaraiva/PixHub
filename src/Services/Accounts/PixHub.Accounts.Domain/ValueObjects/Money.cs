using System.Globalization;
using PixHub.BuildingBlocks.Domain.Primitives;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.ValueObjects;

/// <summary>
/// Monetary amount with an explicit currency. Immutable value object with at most 2 decimal places
/// (centavos). Arithmetic across different currencies is a programming error and throws, whereas
/// business rules (e.g. sufficient funds) are decided by the <c>Account</c> aggregate.
/// </summary>
public sealed class Money : ValueObject
{
    public static readonly Money Zero = new(0m, Currency.BRL);

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public bool IsPositive => Amount > 0m;

    public bool IsZero => Amount == 0m;

    public static Result<Money> Create(decimal amount, Currency currency = Currency.BRL)
    {
        if (amount != decimal.Round(amount, 2, MidpointRounding.ToEven))
        {
            return MoneyErrors.InvalidScale;
        }

        return new Money(amount, currency);
    }

    /// <summary>Builds an amount from an integer number of minor units (e.g. centavos).</summary>
    public static Money FromMinorUnits(long minorUnits, Currency currency = Currency.BRL) =>
        new(minorUnits / 100m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsGreaterThanOrEqualTo(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on amounts of different currencies: {Currency} and {other.Currency}.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() =>
        $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";
}

public static class MoneyErrors
{
    public static readonly Error InvalidScale = Error.Validation(
        "Money.InvalidScale", "O valor monetário não pode ter mais de 2 casas decimais.");
}
