using NSubstitute;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.BlockAccount;
using PixHub.Accounts.Application.Accounts.CloseAccount;
using PixHub.Accounts.Application.Accounts.UnblockAccount;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Accounts;

/// <summary>Comandos de ciclo de vida da conta: bloquear, desbloquear e encerrar.</summary>
public class LifecycleCommandHandlerTests
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

    [Fact]
    public async Task Block_OnActiveAccount_BlocksAndPersists()
    {
        var account = ActiveAccount();
        GivenExistingAccount(account);
        var handler = new BlockAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new BlockAccountCommand(account.Id.Value, "ordem judicial"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Blocked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Block_OnClosedAccount_Fails()
    {
        var account = ActiveAccount();
        account.Close();
        GivenExistingAccount(account);
        var handler = new BlockAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new BlockAccountCommand(account.Id.Value, "ordem judicial"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.AlreadyClosed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unblock_OnBlockedAccount_ReactivatesAccount()
    {
        var account = ActiveAccount();
        account.Block("revisão de risco");
        GivenExistingAccount(account);
        var handler = new UnblockAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new UnblockAccountCommand(account.Id.Value),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_WithZeroBalance_ClosesAndPersists()
    {
        var account = ActiveAccount();
        GivenExistingAccount(account);
        var handler = new CloseAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new CloseAccountCommand(account.Id.Value),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Closed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_WithRemainingBalance_Fails()
    {
        var account = ActiveAccount(10m);
        GivenExistingAccount(account);
        var handler = new CloseAccountCommandHandler(_accountRepository, _unitOfWork);

        var result = await handler.Handle(
            new CloseAccountCommand(account.Id.Value),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.BalanceNotZeroOnClose);
        account.Status.ShouldBe(AccountStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_WhenAccountDoesNotExist_FailsWithNotFound()
    {
        _accountRepository
            .GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);
        var handler = new CloseAccountCommandHandler(_accountRepository, _unitOfWork);
        var missingId = Guid.CreateVersion7();

        var result = await handler.Handle(
            new CloseAccountCommand(missingId),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.NotFound(missingId));
    }
}
