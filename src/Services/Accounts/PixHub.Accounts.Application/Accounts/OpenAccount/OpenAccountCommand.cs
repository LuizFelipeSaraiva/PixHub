using PixHub.Accounts.Application.Abstractions.Messaging;

namespace PixHub.Accounts.Application.Accounts.OpenAccount;

/// <summary>
/// Abre uma nova conta digital para um titular.
/// </summary>
/// <param name="HolderCpf">CPF do titular, com ou sem pontuação — é normalizado pelo value object.</param>
/// <param name="HolderName">Nome do titular.</param>
/// <param name="OpeningBalance">Depósito inicial opcional; ausente significa abrir com saldo zero.</param>
public sealed record OpenAccountCommand(
    string HolderCpf,
    string HolderName,
    decimal? OpeningBalance = null) : ICommand<Guid>;
