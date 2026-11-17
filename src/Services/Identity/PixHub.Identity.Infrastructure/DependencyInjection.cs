using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PixHub.Identity.Infrastructure;

/// <summary>
/// Composition root da camada de Infrastructure: quem hospeda o serviço (Api) chama apenas
/// <see cref="AddIdentityInfrastructure"/> e continua a configuração do OpenIddict só com
/// <c>.AddServer(...)</c> — o registro de <c>.AddCore(...)</c> (as stores do EF Core) já fica aqui,
/// perto do DbContext que ele usa.
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "IdentityDb";

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada.");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"));

            // Exigido pelo OpenIddict: as entidades geradas para ele precisam estar visíveis ao
            // resolver o modelo, mesmo vindas de um assembly diferente do DbContext.
            options.UseOpenIddict();
        });

        services.AddOpenIddict()
            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<IdentityDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(name: "identity-db", tags: ["ready"]);

        services.AddHostedService<ClientsSeeder>();

        return services;
    }
}
