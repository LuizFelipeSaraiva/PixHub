using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.Accounts.Domain.Accounts;

namespace PixHub.Accounts.Application.Abstractions.Data;

/// <summary>
/// Porta do <b>lado de leitura</b>: devolve DTOs projetados diretamente em SQL, sem passar pelo
/// agregado nem pelo change tracker. Será implementada com Dapper na camada de Infrastructure — é o
/// outro lado do CQRS, deliberadamente separado de <see cref="IAccountRepository"/>.
/// </summary>
public interface IAccountReadRepository
{
    /// <summary>Devolve a conta projetada ou <c>null</c> se ela não existir.</summary>
    Task<AccountResponse?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>Lista contas paginadas, opcionalmente filtrando por situação.</summary>
    Task<PagedList<AccountSummaryResponse>> GetPagedAsync(
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
