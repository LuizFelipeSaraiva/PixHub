using PixHub.BuildingBlocks.Domain.Primitives;

namespace PixHub.Accounts.Domain.Accounts;

// Domain events carry primitive-friendly data so they serialize cleanly into the transactional
// outbox and onto the message bus (as integration events) without leaking value-object types.

public sealed record AccountOpened(
    Guid AccountId,
    string HolderCpf,
    string HolderName,
    decimal OpeningBalance,
    string Currency) : DomainEvent;

public sealed record AccountCredited(
    Guid AccountId,
    decimal Amount,
    decimal NewBalance) : DomainEvent;

public sealed record AccountDebited(
    Guid AccountId,
    decimal Amount,
    decimal NewBalance) : DomainEvent;

public sealed record AccountBlocked(
    Guid AccountId,
    string Reason) : DomainEvent;

public sealed record AccountClosed(
    Guid AccountId) : DomainEvent;
