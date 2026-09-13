using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Infrastructure.Repositories;

namespace PixHub.Accounts.Infrastructure;

/// <summary>
/// Composition root da camada de Infrastructure: quem hospeda o serviço (Api/Worker) chama apenas
/// <see cref="AddAccountsInfrastructure"/> e não precisa conhecer EF Core, Npgsql nem Dapper.
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "AccountsDb";

    public static IServiceCollection AddAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada.");

        // Um único NpgsqlDataSource (singleton) é compartilhado pelo EF Core (write) e pelo Dapper
        // (read): mantém um único pool de conexões por serviço em vez de duplicá-lo por lado do CQRS.
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddDbContext<AccountsDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "accounts")));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountReadRepository, AccountReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHealthChecks()
            .AddDbContextCheck<AccountsDbContext>(name: "accounts-db", tags: ["ready"]);

        return services;
    }
}
