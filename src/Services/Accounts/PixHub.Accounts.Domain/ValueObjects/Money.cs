using System.Globalization;
using PixHub.BuildingBlocks.Domain.Primitives;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.ValueObjects;

/// <summary>
/// Valor monetário com moeda explícita. Value object imutável, com no máximo 2 casas decimais
/// (centavos). Operar aritmética entre moedas diferentes é erro de programação e lança exceção; já
/// as regras de negócio (por exemplo, saldo suficiente) são decididas pelo agregado <c>Account</c>.
/// </summary>
public sealed class Money : ValueObject
{
    // Propriedade (não campo estático) de propósito: devolve uma instância nova a cada chamada.
    // Um singleton compartilhado quebraria o rastreamento de tipos possuídos do EF Core, que exige
    // uma instância de Money exclusiva por Account (a mesma referência em vários agregados faz o
    // change tracker perder a trilha e gravar null em balance_amount nos agregados subsequentes).
    public static Money Zero => new(0m, Currency.BRL);

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

    /// <summary>Monta um valor a partir de um inteiro em unidades menores (centavos).</summary>
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
                $"Não é possível operar valores de moedas diferentes: {Currency} e {other.Currency}.");
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
