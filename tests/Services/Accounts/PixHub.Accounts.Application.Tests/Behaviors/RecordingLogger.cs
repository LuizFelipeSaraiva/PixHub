using Microsoft.Extensions.Logging;

namespace PixHub.Accounts.Application.Tests.Behaviors;

/// <summary>
/// <see cref="ILogger{TCategoryName}"/> de teste que guarda o que foi registrado.
/// </summary>
/// <remarks>
/// Um dublê escrito à mão em vez de um mock: o NSubstitute usa proxy dinâmico e não consegue gerar
/// um proxy para <c>ILogger&lt;T&gt;</c> quando <c>T</c> é um tipo <c>internal</c> — que é o caso
/// dos behaviors. Escrever as poucas linhas abaixo é mais simples do que expor os internals para o
/// assembly de proxies só para poder mockar.
/// </remarks>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> _entries = [];

    public IReadOnlyList<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, eventId, formatter(state, exception), exception));
}
