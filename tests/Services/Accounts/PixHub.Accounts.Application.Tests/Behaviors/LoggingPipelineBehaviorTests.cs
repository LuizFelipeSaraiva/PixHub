using Microsoft.Extensions.Logging;
using PixHub.Accounts.Application.Accounts.CreditAccount;
using PixHub.Accounts.Application.Behaviors;
using PixHub.Accounts.Domain.Accounts;
using PixHub.BuildingBlocks.Domain.Results;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Behaviors;

public class LoggingPipelineBehaviorTests
{
    private readonly RecordingLogger<LoggingPipelineBehavior<CreditAccountCommand, Result>> _logger = new();

    private static CreditAccountCommand AnyCommand => new(Guid.CreateVersion7(), 10m);

    private LoggingPipelineBehavior<CreditAccountCommand, Result> CreateBehavior() => new(_logger);

    [Fact]
    public async Task Handle_OnSuccess_ReturnsTheHandlerResultUnchanged()
    {
        var expected = Result.Success();

        ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken) =>
            ValueTask.FromResult(expected);

        var result = await CreateBehavior().Handle(AnyCommand, Next, TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(expected);
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Handle_OnBusinessFailure_LogsAsWarningAndReturnsTheFailure()
    {
        // Saldo insuficiente é desfecho previsto do domínio: precisa sair como Warning, para não
        // disparar o alarme de erro junto com defeitos de verdade.
        var expected = Result.Failure(AccountErrors.InsufficientFunds);

        ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken) =>
            ValueTask.FromResult(expected);

        var result = await CreateBehavior().Handle(AnyCommand, Next, TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(expected);
        result.Error.ShouldBe(AccountErrors.InsufficientFunds);

        var entry = _logger.Entries.ShouldHaveSingleItem(entry => entry.Level == LogLevel.Warning);
        entry.Message.ShouldContain(AccountErrors.InsufficientFunds.Code);
        _logger.Entries.ShouldNotContain(logEntry => logEntry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsAsErrorAndRethrows()
    {
        static ValueTask<Result> Next(CreditAccountCommand message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("falha inesperada");

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await CreateBehavior().Handle(AnyCommand, Next, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("falha inesperada");

        var entry = _logger.Entries.ShouldHaveSingleItem(entry => entry.Level == LogLevel.Error);
        entry.Exception.ShouldBeSameAs(exception);
    }
}

file static class LoggerEntryAssertions
{
    /// <summary>Seleciona as entradas que casam com o filtro e exige exatamente uma.</summary>
    public static T ShouldHaveSingleItem<T>(this IReadOnlyList<T> entries, Func<T, bool> predicate) =>
        entries.Where(predicate).ToArray().ShouldHaveSingleItem();
}
