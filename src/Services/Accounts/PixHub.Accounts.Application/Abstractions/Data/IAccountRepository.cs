using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;

namespace PixHub.Accounts.Application.Abstractions.Data;

/// <summary>
/// Porta do <b>lado de escrita</b>: devolve o agregado <see cref="Account"/> completo, para que as
/// invariantes sejam aplicadas pelo próprio domínio. Será implementada com EF Core (rastreamento de
/// mudanças + migrations) na camada de Infrastructure.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Carrega o agregado para alteração. Devolve <c>null</c> se a conta não existir.</summary>
    Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unicidade entre agregados: uma consulta enxuta (sem materializar o agregado) usada antes de
    /// abrir uma conta. O índice único no banco continua sendo a garantia definitiva contra corrida.
    /// </summary>
    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);

    /// <summary>Registra um novo agregado para inserção na próxima gravação.</summary>
    void Add(Account account);
}
