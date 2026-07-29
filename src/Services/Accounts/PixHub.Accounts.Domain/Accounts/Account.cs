using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Primitives;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.Accounts;

/// <summary>
/// A raiz de agregado Account — o limite transacional do saldo de um cliente. Toda alteração de
/// estado passa por métodos que revelam a intenção, aplicam as invariantes e devolvem um
/// <see cref="Result"/>; cada alteração bem-sucedida registra um domain event para o outbox.
/// </summary>
public sealed class Account : AggregateRoot<AccountId>
{
    // Exigido pelo EF Core para materialização; o código de domínio deve usar a fábrica/comportamentos.
    private Account()
    {
    }

    private Account(AccountId id, Cpf holderCpf, string holderName, Money openingBalance)
        : base(id)
    {
        HolderCpf = holderCpf;
        HolderName = holderName;
        Balance = openingBalance;
        Status = AccountStatus.Active;
        OpenedAtUtc = DateTimeOffset.UtcNow;
    }

    public Cpf HolderCpf { get; private set; } = null!;

    public string HolderName { get; private set; } = null!;

    public Money Balance { get; private set; } = Money.Zero;

    public AccountStatus Status { get; private set; }

    public DateTimeOffset OpenedAtUtc { get; private set; }

    /// <summary>Abre uma nova conta ativa. O saldo de abertura, quando omitido, é zero.</summary>
    public static Result<Account> Open(Cpf holderCpf, string holderName, Money? openingBalance = null)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            return AccountErrors.EmptyHolderName;
        }

        var opening = openingBalance ?? Money.Zero;
        if (opening.Amount < 0m)
        {
            return AccountErrors.NegativeOpeningBalance;
        }

        var account = new Account(AccountId.New(), holderCpf, holderName.Trim(), opening);
        account.RaiseDomainEvent(new AccountOpened(
            account.Id.Value,
            holderCpf.Value,
            account.HolderName,
            opening.Amount,
            opening.Currency.ToString()));

        return account;
    }

    public Result Credit(Money amount)
    {
        if (Status != AccountStatus.Active)
        {
            return AccountErrors.InactiveAccount;
        }

        if (!amount.IsPositive)
        {
            return AccountErrors.NonPositiveAmount;
        }

        Balance = Balance.Add(amount);
        RaiseDomainEvent(new AccountCredited(Id.Value, amount.Amount, Balance.Amount));
        return Result.Success();
    }

    public Result Debit(Money amount)
    {
        if (Status != AccountStatus.Active)
        {
            return AccountErrors.InactiveAccount;
        }

        if (!amount.IsPositive)
        {
            return AccountErrors.NonPositiveAmount;
        }

        if (!Balance.IsGreaterThanOrEqualTo(amount))
        {
            return AccountErrors.InsufficientFunds;
        }

        Balance = Balance.Subtract(amount);
        RaiseDomainEvent(new AccountDebited(Id.Value, amount.Amount, Balance.Amount));
        return Result.Success();
    }

    public Result Block(string reason)
    {
        if (Status == AccountStatus.Closed)
        {
            return AccountErrors.AlreadyClosed;
        }

        if (Status == AccountStatus.Blocked)
        {
            return Result.Success(); // idempotente: bloquear conta já bloqueada não faz nada
        }

        Status = AccountStatus.Blocked;
        RaiseDomainEvent(new AccountBlocked(Id.Value, reason));
        return Result.Success();
    }

    public Result Unblock()
    {
        if (Status == AccountStatus.Closed)
        {
            return AccountErrors.AlreadyClosed;
        }

        Status = AccountStatus.Active;
        return Result.Success();
    }

    public Result Close()
    {
        if (Status == AccountStatus.Closed)
        {
            return Result.Success(); // idempotente
        }

        if (!Balance.IsZero)
        {
            return AccountErrors.BalanceNotZeroOnClose;
        }

        Status = AccountStatus.Closed;
        RaiseDomainEvent(new AccountClosed(Id.Value));
        return Result.Success();
    }
}
