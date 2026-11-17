using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PixHub.Identity.Infrastructure;

/// <summary>
/// Fábrica usada só pelas ferramentas de design-time (<c>dotnet ef migrations add</c>). O host real
/// (Api) nunca passa por aqui — ele monta o <see cref="IdentityDbContext"/> via
/// <see cref="DependencyInjection.AddIdentityInfrastructure"/>, com a connection string vinda da
/// configuração do ambiente.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("IDENTITYDB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=pixhub_identity;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
