using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.Accounts;

public static class AccountErrors
{
    public static readonly Error EmptyHolderName = Error.Validation(
        "Account.EmptyHolderName", "O nome do titular é obrigatório.");

    public static readonly Error NegativeOpeningBalance = Error.Validation(
        "Account.NegativeOpeningBalance", "O saldo de abertura não pode ser negativo.");

    public static readonly Error InactiveAccount = Error.Conflict(
        "Account.Inactive", "A conta não está ativa e não aceita movimentações.");

    public static readonly Error NonPositiveAmount = Error.Validation(
        "Account.NonPositiveAmount", "O valor da operação deve ser positivo.");

    public static readonly Error InsufficientFunds = Error.Conflict(
        "Account.InsufficientFunds", "Saldo insuficiente para a operação.");

    public static readonly Error AlreadyClosed = Error.Conflict(
        "Account.AlreadyClosed", "A conta já está encerrada.");

    public static readonly Error BalanceNotZeroOnClose = Error.Conflict(
        "Account.BalanceNotZeroOnClose", "A conta só pode ser encerrada com saldo zero.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Account.NotFound", $"Conta {id} não encontrada.");
}
