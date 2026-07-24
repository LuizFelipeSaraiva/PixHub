namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// An aggregate root is the only entry point for mutating an aggregate and the boundary of a
/// transaction. It records <see cref="IDomainEvent"/>s that infrastructure collects and publishes
/// (through the transactional outbox) after the aggregate is saved.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
