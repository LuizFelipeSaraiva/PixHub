using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Abstractions.Messaging;

/// <summary>Trata um <see cref="ICommand"/>.</summary>
public interface ICommandHandler<in TCommand> : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>Trata um <see cref="ICommand{TResponse}"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
