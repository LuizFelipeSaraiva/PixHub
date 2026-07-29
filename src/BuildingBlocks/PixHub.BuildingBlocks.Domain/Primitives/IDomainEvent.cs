namespace PixHub.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Um fato que aconteceu dentro de um agregado. Domain events são levantados pelos agregados e
/// despachados depois que o agregado é persistido (via outbox), desacoplando efeitos colaterais e
/// viabilizando tanto sagas coreografadas quanto orquestradas.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Record base de conveniência que carimba id único e data/hora em cada evento.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
