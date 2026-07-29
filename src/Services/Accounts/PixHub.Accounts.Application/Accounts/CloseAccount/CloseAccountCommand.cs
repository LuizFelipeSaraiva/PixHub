using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.CloseAccount;

/// <summary>
/// Encerra a conta. O agregado só permite o encerramento com saldo zero, de modo que nenhum recurso
/// do cliente fique preso em uma conta encerrada.
/// </summary>
public sealed record CloseAccountCommand(Guid AccountId) : ICommand;
