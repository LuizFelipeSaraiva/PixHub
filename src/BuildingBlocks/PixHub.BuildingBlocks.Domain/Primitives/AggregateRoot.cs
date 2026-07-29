namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// A raiz de agregado é o único ponto de entrada para alterar um agregado e o limite de uma
/// transação. Ela acumula <see cref="IDomainEvent"/>s que a infraestrutura coleta e publica
/// (pelo outbox transacional) depois que o agregado é gravado.
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
