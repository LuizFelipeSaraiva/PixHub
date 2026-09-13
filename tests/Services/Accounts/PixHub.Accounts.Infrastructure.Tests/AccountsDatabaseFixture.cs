using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace PixHub.Accounts.Infrastructure.Tests;

/// <summary>
/// Sobe um Postgres real (Testcontainers) uma única vez por classe de teste e aplica as migrations
/// do projeto — os testes de integração exercitam o SQL de verdade (EF Core e Dapper), não um
/// provider in-memory que mascararia diferenças de tradução de query.
/// </summary>
public sealed class AccountsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pixhub_accounts_tests")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        DataSource = new NpgsqlDataSourceBuilder(_container.GetConnectionString()).Build();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Um <see cref="AccountsDbContext"/> novo por chamada — cada teste tem seu próprio change tracker.</summary>
    public AccountsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql(DataSource, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "accounts"))
            .Options;

        return new AccountsDbContext(options);
    }
}
