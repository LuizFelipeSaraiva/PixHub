using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Abstractions.Messaging;

/// <summary>Trata uma <see cref="IQuery{TResponse}"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse> : Mediator.IQueryHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
