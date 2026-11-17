using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PixHub.Identity.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace PixHub.Identity.Infrastructure.Tests;

/// <summary>
/// Sobe um Postgres real (Testcontainers), aplica as migrations e então hospeda a
/// <c>PixHub.Identity.Api</c> completa via <see cref="WebApplicationFactory{TEntryPoint}"/> — o teste
/// exercita o fluxo OAuth2 de ponta a ponta (endpoint HTTP real, OpenIddict, EF Core), não só a
/// camada de dados isolada.
/// </summary>
/// <remarks>
/// A connection string e o segredo do client são passados por variável de ambiente, não por
/// <c>ConfigureWebHost</c>/<c>ConfigureAppConfiguration</c>: o <c>Program.cs</c> de Identity.Api lê
/// <c>IConfiguration</c> de forma eager (antes de <c>builder.Build()</c>, dentro de
/// <c>AddIdentityInfrastructure</c>) para resolver a connection string — nesse ponto do fluxo, os
/// overrides que o <see cref="WebApplicationFactory{TEntryPoint}"/> injeta via
/// <c>ConfigureWebHost</c> (aplicados perto do <c>Build()</c> interceptado) ainda não estão visíveis.
/// Variável de ambiente é lida direto por <c>WebApplication.CreateBuilder</c>, no início do processo.
/// </remarks>
public sealed class IdentityApiFixture : WebApplicationFactory<PixHub.Identity.Api.Program>, IAsyncLifetime
{
    /// <summary>Segredo de teste do client M2M seedado pelo <see cref="ClientsSeeder"/> — não é usado fora deste processo de teste.</summary>
    public const string BackendClientSecret = "test-secret-not-for-production";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pixhub_identity_tests")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", _container.GetConnectionString());
        Environment.SetEnvironmentVariable("Identity__Clients__BackendClient__Secret", BackendClientSecret);

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_container.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        await using var dbContext = new IdentityDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
