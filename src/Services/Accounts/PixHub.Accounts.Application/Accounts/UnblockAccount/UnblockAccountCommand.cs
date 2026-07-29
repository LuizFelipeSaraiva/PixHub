using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.UnblockAccount;

/// <summary>Reativa uma conta bloqueada, voltando a permitir movimentações.</summary>
public sealed record UnblockAccountCommand(Guid AccountId) : ICommand;
