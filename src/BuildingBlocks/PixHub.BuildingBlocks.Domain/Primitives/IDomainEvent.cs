namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// A fact that has happened inside an aggregate. Domain events are raised by aggregates and
/// dispatched after the aggregate is persisted (via the outbox), decoupling side effects and
/// enabling both choreographed and orchestrated sagas.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Convenience base record that stamps a unique id and timestamp on each event.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
