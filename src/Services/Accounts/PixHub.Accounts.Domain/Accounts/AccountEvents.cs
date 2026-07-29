using PixHub.BuildingBlocks.Domain.Primitives;

namespace PixHub.Accounts.Domain.Accounts;

// Os domain events carregam dados primitivos para serializar de forma limpa no outbox transacional
// e no barramento de mensagens (como integration events), sem vazar os tipos de value object.

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
