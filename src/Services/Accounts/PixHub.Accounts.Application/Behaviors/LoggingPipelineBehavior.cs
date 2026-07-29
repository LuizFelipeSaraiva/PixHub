using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Behaviors;

/// <summary>
/// Registra em log o início, o desfecho e a duração de cada caso de uso.
/// </summary>
/// <remarks>
/// Uma falha de negócio (<see cref="Result.IsFailure"/>) sai como <c>Warning</c> com o código do
/// erro, e não como <c>Error</c>: saldo insuficiente é um desfecho previsto do domínio, não um
/// defeito — separar os dois evita poluir o alarme de erro com ruído esperado. Exceções, aí sim,
/// saem como <c>Error</c> e continuam subindo.
/// </remarks>
internal sealed class LoggingPipelineBehavior<TMessage, TResponse>(
    ILogger<LoggingPipelineBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var useCase = typeof(TMessage).Name;
        var timestamp = Stopwatch.GetTimestamp();

        ApplicationLog.UseCaseStarted(logger, useCase);

        try
        {
            var response = await next(message, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

            if (response.IsSuccess)
            {
                ApplicationLog.UseCaseSucceeded(logger, useCase, elapsedMs);
            }
            else
            {
                ApplicationLog.UseCaseFailed(logger, useCase, response.Error.Code, response.Error.Description, elapsedMs);
            }

            return response;
        }
        catch (Exception exception)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            ApplicationLog.UseCaseThrew(logger, useCase, elapsedMs, exception);
            throw;
        }
    }
}

/// <summary>
/// Mensagens de log geradas em tempo de compilação (<c>[LoggerMessage]</c>): evitam boxing e a
/// formatação da mensagem quando o nível está desligado. Ficam em uma classe não genérica porque o
/// gerador do <c>LoggerMessage</c> não emite código para classes genéricas.
/// </summary>
internal static partial class ApplicationLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Executando caso de uso {UseCase}")]
    public static partial void UseCaseStarted(ILogger logger, string useCase);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Caso de uso {UseCase} concluído em {ElapsedMilliseconds:F1} ms")]
    public static partial void UseCaseSucceeded(ILogger logger, string useCase, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Caso de uso {UseCase} recusado ({ErrorCode}: {ErrorDescription}) em {ElapsedMilliseconds:F1} ms")]
    public static partial void UseCaseFailed(
        ILogger logger,
        string useCase,
        string errorCode,
        string errorDescription,
        double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Caso de uso {UseCase} lançou exceção após {ElapsedMilliseconds:F1} ms")]
    public static partial void UseCaseThrew(
        ILogger logger,
        string useCase,
        double elapsedMilliseconds,
        Exception exception);
}
