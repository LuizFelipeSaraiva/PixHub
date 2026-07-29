using FluentValidation;
using PixHub.Accounts.Application.Accounts.CreditAccount;
using PixHub.Accounts.Application.Accounts.OpenAccount;
using PixHub.Accounts.Application.Behaviors;
using PixHub.BuildingBlocks.Domain.Results;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Behaviors;

public class ValidationPipelineBehaviorTests
{
    private const string ValidCpf = "111.444.777-35";

    [Fact]
    public async Task Handle_WithNoValidatorsRegistered_InvokesTheHandler()
    {
        var behavior = new ValidationPipelineBehavior<CreditAccountCommand, Result>([]);
        var handlerCalled = false;

        ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken)
        {
            handlerCalled = true;
            return ValueTask.FromResult(Result.Success());
        }

        var result = await behavior.Handle(
            new CreditAccountCommand(Guid.CreateVersion7(), 10m),
            Next,
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithValidMessage_InvokesTheHandler()
    {
        var behavior = new ValidationPipelineBehavior<CreditAccountCommand, Result>(
            [new CreditAccountCommandValidator()]);
        var handlerCalled = false;

        ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken)
        {
            handlerCalled = true;
            return ValueTask.FromResult(Result.Success());
        }

        var result = await behavior.Handle(
            new CreditAccountCommand(Guid.CreateVersion7(), 10m),
            Next,
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidMessage_ShortCircuitsWithValidationError()
    {
        var behavior = new ValidationPipelineBehavior<CreditAccountCommand, Result>(
            [new CreditAccountCommandValidator()]);
        var handlerCalled = false;

        ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken)
        {
            handlerCalled = true;
            return ValueTask.FromResult(Result.Success());
        }

        // Valor zero viola "deve ser positivo" e o id vazio viola "obrigatório".
        var result = await behavior.Handle(
            new CreditAccountCommand(Guid.Empty, 0m),
            Next,
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();

        var validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Type.ShouldBe(ErrorType.Validation);
        validationError.Errors.Count.ShouldBe(2);
        validationError.Errors.Select(error => error.Code)
            .ShouldBe([nameof(CreditAccountCommand.AccountId), nameof(CreditAccountCommand.Amount)], ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_WhenResponseIsGeneric_BuildsAFailedResultOfThatType()
    {
        // Este é o caso que a fábrica por reflexão precisa acertar: TResponse é Result<Guid>,
        // e não o Result não genérico.
        var behavior = new ValidationPipelineBehavior<OpenAccountCommand, Result<Guid>>(
            [new OpenAccountCommandValidator()]);
        var handlerCalled = false;

        ValueTask<Result<Guid>> Next(OpenAccountCommand message, CancellationToken cancellationToken)
        {
            handlerCalled = true;
            return ValueTask.FromResult(Result.Success(Guid.CreateVersion7()));
        }

        var result = await behavior.Handle(
            new OpenAccountCommand(string.Empty, string.Empty),
            Next,
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeFalse();
        result.ShouldBeOfType<Result<Guid>>();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>().Errors.Count.ShouldBe(2);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public async Task Handle_WhenResponseIsGenericAndMessageIsValid_InvokesTheHandler()
    {
        var behavior = new ValidationPipelineBehavior<OpenAccountCommand, Result<Guid>>(
            [new OpenAccountCommandValidator()]);
        var expectedId = Guid.CreateVersion7();

        ValueTask<Result<Guid>> Next(OpenAccountCommand message, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success(expectedId));

        var result = await behavior.Handle(
            new OpenAccountCommand(ValidCpf, "Maria Silva"),
            Next,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Handle_AggregatesFailuresFromEveryRegisteredValidator()
    {
        var behavior = new ValidationPipelineBehavior<CreditAccountCommand, Result>(
            [new CreditAccountCommandValidator(), new AlwaysFailsValidator()]);

        static ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(
            new CreditAccountCommand(Guid.Empty, 0m),
            Next,
            TestContext.Current.CancellationToken);

        var validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.Count.ShouldBe(3);
    }

    private sealed class AlwaysFailsValidator : AbstractValidator<CreditAccountCommand>
    {
        public AlwaysFailsValidator() =>
            RuleFor(command => command.AccountId)
                .Must(_ => false).WithMessage("Regra extra sempre reprovada.");
    }
}
