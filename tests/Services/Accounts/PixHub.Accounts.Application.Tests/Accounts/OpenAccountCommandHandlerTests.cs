using NSubstitute;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.OpenAccount;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Accounts;

public class OpenAccountCommandHandlerTests
{
    private const string ValidCpf = "111.444.777-35";

    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OpenAccountCommandHandler _handler;

    public OpenAccountCommandHandlerTests() =>
        _handler = new OpenAccountCommandHandler(_accountRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidCommand_AddsAccountAndReturnsItsId()
    {
        Account? added = null;
        _accountRepository.Add(Arg.Do<Account>(account => added = account));

        var result = await _handler.Handle(new OpenAccountCommand(ValidCpf, "Maria Silva"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        added.ShouldNotBeNull();
        result.Value.ShouldBe(added.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOpeningBalance_OpensAccountWithThatBalance()
    {
        Account? added = null;
        _accountRepository.Add(Arg.Do<Account>(account => added = account));

        var result = await _handler.Handle(
            new OpenAccountCommand(ValidCpf, "Maria Silva", 250.75m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        added.ShouldNotBeNull();
        added.Balance.Amount.ShouldBe(250.75m);
        added.Status.ShouldBe(AccountStatus.Active);
    }

    [Fact]
    public async Task Handle_WithoutOpeningBalance_OpensAccountWithZero()
    {
        Account? added = null;
        _accountRepository.Add(Arg.Do<Account>(account => added = account));

        await _handler.Handle(new OpenAccountCommand(ValidCpf, "Maria Silva"), TestContext.Current.CancellationToken);

        added.ShouldNotBeNull();
        added.Balance.IsZero.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidCpf_FailsWithoutTouchingPersistence()
    {
        var result = await _handler.Handle(
            new OpenAccountCommand("111.444.777-00", "Maria Silva"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CpfErrors.InvalidCheckDigits);
        _accountRepository.DidNotReceive().Add(Arg.Any<Account>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCpfAlreadyHasAnAccount_FailsWithConflict()
    {
        _accountRepository
            .ExistsByCpfAsync(Arg.Any<Cpf>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new OpenAccountCommand(ValidCpf, "Maria Silva"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.DuplicateCpf);
        _accountRepository.DidNotReceive().Add(Arg.Any<Account>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMoreThanTwoDecimalPlaces_FailsWithInvalidScale()
    {
        var result = await _handler.Handle(
            new OpenAccountCommand(ValidCpf, "Maria Silva", 10.999m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MoneyErrors.InvalidScale);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyHolderName_SurfacesTheDomainError()
    {
        // O handler não duplica a regra: quem recusa é o agregado, e o erro sobe intacto.
        var result = await _handler.Handle(
            new OpenAccountCommand(ValidCpf, "   "),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.EmptyHolderName);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
