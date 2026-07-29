using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.BlockAccount;

/// <summary>
/// Bloqueia a conta, impedindo novas movimentações — usado por decisão de risco/antifraude ou por
/// determinação judicial. O motivo é obrigatório porque alimenta a trilha de auditoria.
/// </summary>
public sealed record BlockAccountCommand(Guid AccountId, string Reason) : ICommand;
