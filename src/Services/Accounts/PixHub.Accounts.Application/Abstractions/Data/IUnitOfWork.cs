namespace PixHub.Accounts.Application.Abstractions.Data;

/// <summary>
/// Confirma, em uma única transação, as alterações feitas no agregado. A implementação (EF Core)
/// também é o ponto onde os domain events acumulados viram mensagens do outbox — por isso o
/// handler grava explicitamente pela unidade de trabalho em vez de o repositório salvar sozinho.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
