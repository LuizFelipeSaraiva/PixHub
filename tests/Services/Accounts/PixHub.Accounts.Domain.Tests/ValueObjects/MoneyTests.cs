using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10.50)]
    [InlineData(1000)]
    [InlineData(-25.99)]
    public void Create_WithAtMostTwoDecimals_Succeeds(decimal amount)
    {
        var result = Money.Create(amount);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(amount);
        result.Value.Currency.ShouldBe(Currency.BRL);
    }

    [Theory]
    [InlineData(10.123)]
    [InlineData(0.001)]
    public void Create_WithMoreThanTwoDecimals_FailsWithInvalidScale(decimal amount)
    {
        var result = Money.Create(amount);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MoneyErrors.InvalidScale);
    }

    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var ten = Money.Create(10m).Value;
        var five = Money.Create(5m).Value;

        ten.Add(five).Amount.ShouldBe(15m);
    }

    [Fact]
    public void Subtract_SameCurrency_SubtractsAmounts()
    {
        var ten = Money.Create(10m).Value;
        var five = Money.Create(5m).Value;

        ten.Subtract(five).Amount.ShouldBe(5m);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var brl = Money.Create(10m, Currency.BRL).Value;
        var usd = Money.Create(10m, Currency.USD).Value;

        Should.Throw<InvalidOperationException>(() => brl.Add(usd));
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_ComparesAmounts()
    {
        var ten = Money.Create(10m).Value;
        var five = Money.Create(5m).Value;

        ten.IsGreaterThanOrEqualTo(five).ShouldBeTrue();
        five.IsGreaterThanOrEqualTo(ten).ShouldBeFalse();
        ten.IsGreaterThanOrEqualTo(ten).ShouldBeTrue();
    }

    [Fact]
    public void FromMinorUnits_ConvertsCentavos()
    {
        Money.FromMinorUnits(12345).Amount.ShouldBe(123.45m);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Money.Create(10m).Value;
        var b = Money.Create(10m).Value;
        var c = Money.Create(10m, Currency.USD).Value;

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.ShouldNotBe(c);
    }
}
