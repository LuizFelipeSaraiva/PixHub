using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.DebitAccount;

/// <summary>
/// Debita um valor da conta (saque ou perna de débito de uma transferência Pix).
/// </summary>
/// <remarks>
/// Na Saga orquestrada da Fase 4 este é o passo que precisa de compensação: se a contabilização ou
/// a análise de risco falharem depois, o valor debitado é devolvido por um crédito de estorno.
/// </remarks>
public sealed record DebitAccountCommand(Guid AccountId, decimal Amount) : ICommand;
