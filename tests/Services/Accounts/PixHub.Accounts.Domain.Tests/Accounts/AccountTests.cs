using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Domain.Tests.Accounts;

public class AccountTests
{
    private static Cpf ValidCpf => Cpf.Create("111.444.777-35").Value;

    private static Money Brl(decimal amount) => Money.Create(amount).Value;

    private static Account OpenActiveAccount(decimal opening = 0m)
    {
        var account = Account.Open(ValidCpf, "Maria Silva", Brl(opening)).Value;
        account.ClearDomainEvents();
        return account;
    }

    [Fact]
    public void Open_WithValidData_CreatesActiveAccountAndRaisesEvent()
    {
        var result = Account.Open(ValidCpf, "  Maria Silva  ");

        result.IsSuccess.ShouldBeTrue();
        var account = result.Value;
        account.Status.ShouldBe(AccountStatus.Active);
        account.Balance.IsZero.ShouldBeTrue();
        account.HolderName.ShouldBe("Maria Silva"); // trimmed

        var opened = account.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AccountOpened>();
        opened.AccountId.ShouldBe(account.Id.Value);
        opened.HolderCpf.ShouldBe("11144477735");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_WithEmptyName_Fails(string name)
    {
        Account.Open(ValidCpf, name).Error.ShouldBe(AccountErrors.EmptyHolderName);
    }

    [Fact]
    public void Open_WithNegativeOpeningBalance_Fails()
    {
        Account.Open(ValidCpf, "Maria", Brl(-1m)).Error.ShouldBe(AccountErrors.NegativeOpeningBalance);
    }

    [Fact]
    public void Credit_IncreasesBalanceAndRaisesEvent()
    {
        var account = OpenActiveAccount();

        var result = account.Credit(Brl(100m));

        result.IsSuccess.ShouldBeTrue();
        account.Balance.Amount.ShouldBe(100m);
        var credited = account.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AccountCredited>();
        credited.Amount.ShouldBe(100m);
        credited.NewBalance.ShouldBe(100m);
    }

    [Fact]
    public void Credit_WithNonPositiveAmount_Fails()
    {
        var account = OpenActiveAccount();

        account.Credit(Brl(0m)).Error.ShouldBe(AccountErrors.NonPositiveAmount);
    }

    [Fact]
    public void Debit_WithSufficientFunds_DecreasesBalance()
    {
        var account = OpenActiveAccount(200m);

        var result = account.Debit(Brl(50m));

        result.IsSuccess.ShouldBeTrue();
        account.Balance.Amount.ShouldBe(150m);
        account.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AccountDebited>().NewBalance.ShouldBe(150m);
    }

    [Fact]
    public void Debit_WithInsufficientFunds_Fails()
    {
        var account = OpenActiveAccount(30m);

        account.Debit(Brl(50m)).Error.ShouldBe(AccountErrors.InsufficientFunds);
        account.Balance.Amount.ShouldBe(30m); // unchanged
    }

    [Fact]
    public void Debit_OnBlockedAccount_Fails()
    {
        var account = OpenActiveAccount(100m);
        account.Block("suspeita de fraude");

        account.Debit(Brl(10m)).Error.ShouldBe(AccountErrors.InactiveAccount);
    }

    [Fact]
    public void Block_SetsStatusAndIsIdempotent()
    {
        var account = OpenActiveAccount();

        account.Block("motivo").IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Blocked);

        account.ClearDomainEvents();
        account.Block("de novo").IsSuccess.ShouldBeTrue(); // idempotent
        account.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Close_WithZeroBalance_Closes()
    {
        var account = OpenActiveAccount();

        account.Close().IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Closed);
        account.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AccountClosed>();
    }

    [Fact]
    public void Close_WithNonZeroBalance_Fails()
    {
        var account = OpenActiveAccount(10m);

        account.Close().Error.ShouldBe(AccountErrors.BalanceNotZeroOnClose);
        account.Status.ShouldBe(AccountStatus.Active);
    }
}
