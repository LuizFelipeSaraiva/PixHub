using PixHub.Accounts.Application.Abstractions.Data;

namespace PixHub.Accounts.Infrastructure;

/// <summary>
/// Confirma as alterações do <see cref="AccountsDbContext"/> em uma única transação implícita do EF
/// Core. Ponto de extensão futuro: na Fase 3, é aqui que os domain events acumulados no agregado
/// virarão mensagens do outbox transacional, gravadas na mesma transação do <c>SaveChanges</c>.
/// </summary>
internal sealed class UnitOfWork(AccountsDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
