using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Abstractions.Messaging;

// Estas interfaces encapsulam as do pacote Mediator por dois motivos:
//   1. amarram todo caso de uso ao trilho do Result, de modo que um handler não possa lançar
//      exceção silenciosamente para uma falha esperada — e o behavior de validação possa ser
//      restringido a `TResponse : Result`;
//   2. casos de uso e handlers passam a depender de abstrações do PixHub em vez da biblioteca de
//      mediator, o que mantém a camada Application substituível (garantido pelos testes de
//      arquitetura).

/// <summary>Caso de uso que altera estado e não devolve valor além de sucesso/falha.</summary>
public interface ICommand : Mediator.ICommand<Result>, IBaseCommand;

/// <summary>Caso de uso que altera estado e devolve <typeparamref name="TResponse"/> em caso de sucesso.</summary>
public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>, IBaseCommand;

/// <summary>
/// Marcador comum aos dois formatos de comando. Dá aos pipeline behaviors um único tipo para
/// restringir quando precisarem valer somente para comandos (por exemplo, um futuro behavior de
/// transação/outbox).
/// </summary>
public interface IBaseCommand;
