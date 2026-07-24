using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Primitives;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.Accounts;

/// <summary>
/// The Account aggregate root — the transactional boundary for a customer's balance. All state
/// changes go through intention-revealing methods that enforce invariants and return a
/// <see cref="Result"/>, and each successful change records a domain event for the outbox.
/// </summary>
public sealed class Account : AggregateRoot<AccountId>
{
    // Required by EF Core for materialization; domain code must use the factory/behaviors.
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

    /// <summary>Opens a new active account. The opening balance defaults to zero.</summary>
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
            return Result.Success(); // idempotent: blocking a blocked account is a no-op
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
            return Result.Success(); // idempotent
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
