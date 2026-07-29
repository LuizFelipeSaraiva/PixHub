using NSubstitute;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.CreditAccount;
using PixHub.Accounts.Application.Accounts.DebitAccount;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Accounts;

/// <summary>
/// Crédito e débito compartilham o mesmo roteiro (carregar agregado → converter valor → delegar ao
/// domínio → gravar), por isso são exercitados juntos.
/// </summary>
public class MovementCommandHandlerTests
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Account ActiveAccount(decimal openingBalance = 0m) =>
        Account.Open(
            Cpf.Create("111.444.777-35").Value,
            "Maria Silva",
            Money.Create(openingBalance).Value).Value;

    private void GivenExistingAccount(Account account) =>
        _accountRepository
            .GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(account);

    // ----- crédito -----

    [Fact]
    public async Task Credit_OnActiveAccount_IncreasesBalanceAndPersists()
    {
        var account = ActiveAccount();
        GivenExistingAccount(account);
        var handler = new CreditAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new CreditAccountCommand(account.Id.Value, 100m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        account.Balance.Amount.ShouldBe(100m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Credit_WhenAccountDoesNotExist_FailsWithNotFound()
    {
        _accountRepository
            .GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);
        var handler = new CreditAccountCommandHandler(_accountRepository, _unitOfWork);
        var missingId = Guid.CreateVersion7();

        var result = await handler.Handle(
            new CreditAccountCommand(missingId, 100m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.NotFound(missingId));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Credit_OnBlockedAccount_FailsAndDoesNotPersist()
    {
        var account = ActiveAccount();
        account.Block("suspeita de fraude");
        GivenExistingAccount(account);
        var handler = new CreditAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new CreditAccountCommand(account.Id.Value, 100m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.InactiveAccount);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Credit_WithMoreThanTwoDecimalPlaces_FailsWithInvalidScale()
    {
        var account = ActiveAccount();
        GivenExistingAccount(account);
        var handler = new CreditAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new CreditAccountCommand(account.Id.Value, 10.001m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MoneyErrors.InvalidScale);
    }

    // ----- débito -----

    [Fact]
    public async Task Debit_WithSufficientFunds_DecreasesBalanceAndPersists()
    {
        var account = ActiveAccount(500m);
        GivenExistingAccount(account);
        var handler = new DebitAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new DebitAccountCommand(account.Id.Value, 120.50m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        account.Balance.Amount.ShouldBe(379.50m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debit_WithInsufficientFunds_FailsAndLeavesBalanceUntouched()
    {
        var account = ActiveAccount(50m);
        GivenExistingAccount(account);
        var handler = new DebitAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new DebitAccountCommand(account.Id.Value, 80m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.InsufficientFunds);
        account.Balance.Amount.ShouldBe(50m);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debit_WhenAccountDoesNotExist_FailsWithNotFound()
    {
        _accountRepository
            .GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);
        var handler = new DebitAccountCommandHandler(_accountRepository, _unitOfWork);
        var missingId = Guid.CreateVersion7();

        var result = await handler.Handle(
            new DebitAccountCommand(missingId, 10m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.NotFound(missingId));
    }
}
