using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PixHub.Accounts.Infrastructure;

/// <summary>
/// Fábrica usada só pelas ferramentas de design-time (<c>dotnet ef migrations add</c>). O host real
/// (Api/Worker) nunca passa por aqui — ele monta o <see cref="AccountsDbContext"/> via
/// <see cref="DependencyInjection.AddAccountsInfrastructure"/>, com a connection string vinda da
/// configuração do ambiente.
/// </summary>
public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ACCOUNTSDB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=pixhub_accounts;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AccountsDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "accounts"));

        return new AccountsDbContext(optionsBuilder.Options);
    }
}
