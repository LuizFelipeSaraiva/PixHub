using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.CreditAccount;

/// <summary>
/// Credita um valor na conta (depósito ou perna de crédito de uma transferência Pix).
/// </summary>
/// <remarks>
/// A partir da Fase 2 este comando passa a exigir uma chave de idempotência, para que o reenvio de
/// uma mensagem não credite o valor duas vezes.
/// </remarks>
public sealed record CreditAccountCommand(Guid AccountId, decimal Amount) : ICommand;
