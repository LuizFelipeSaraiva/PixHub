using Microsoft.EntityFrameworkCore;
using PixHub.Accounts.Domain.Accounts;

namespace PixHub.Accounts.Infrastructure;

/// <summary>
/// O lado de <b>escrita</b> do CQRS de Accounts. Só o agregado <see cref="Account"/> é rastreado por
/// aqui — consultas passam ao largo do change tracker e são resolvidas via Dapper
/// (<see cref="Repositories.AccountReadRepository"/>), no outro lado do CQRS.
/// </summary>
public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("accounts");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
    }
}
