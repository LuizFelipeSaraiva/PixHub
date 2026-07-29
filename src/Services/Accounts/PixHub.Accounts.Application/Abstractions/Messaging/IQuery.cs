using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Abstractions.Messaging;

/// <summary>
/// Caso de uso somente de leitura. Queries nunca carregam um agregado: vão direto ao lado de
/// leitura (Dapper) e devolvem um DTO — é a metade explícita do CQRS neste serviço.
/// </summary>
public interface IQuery<TResponse> : Mediator.IQuery<Result<TResponse>>;
