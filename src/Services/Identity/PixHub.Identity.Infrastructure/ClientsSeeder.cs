using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PixHub.Identity.Infrastructure;

/// <summary>
/// Garante, na subida do serviço, que os clientes M2M (machine-to-machine) e os escopos que este
/// servidor de autorização conhece existam — idempotente (não recria o que já existe), então é seguro
/// rodar a cada start. Só cobre <c>client_credentials</c> hoje: o fluxo interativo (authorization
/// code + PKCE, para usuário final) fica para uma fase seguinte, quando existir um client de fato.
/// </summary>
/// <param name="serviceScopeFactory">
/// Os managers do OpenIddict são <c>scoped</c>; um <see cref="IHostedService"/> vive fora de qualquer
/// escopo de requisição, então precisamos abrir um escopo manualmente para resolvê-los.
/// </param>
public sealed class ClientsSeeder(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    : IHostedService
{
    /// <summary>Escopo que autoriza chamadas à Api de Accounts — o recurso protegido nesta fase.</summary>
    public const string AccountsApiScope = "accounts.api";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        await EnsureScopeExistsAsync(scopeManager, cancellationToken);

        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await EnsureAccountsServiceClientExistsAsync(applicationManager, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureScopeExistsAsync(
        IOpenIddictScopeManager scopeManager, CancellationToken cancellationToken)
    {
        if (await scopeManager.FindByNameAsync(AccountsApiScope, cancellationToken) is not null)
        {
            return;
        }

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = AccountsApiScope,
            DisplayName = "Acesso à Api de Accounts",
            Resources = { "accounts-api" }
        }, cancellationToken);
    }

    private async Task EnsureAccountsServiceClientExistsAsync(
        IOpenIddictApplicationManager applicationManager, CancellationToken cancellationToken)
    {
        const string clientId = "pixhub-backend-client";

        if (await applicationManager.FindByClientIdAsync(clientId, cancellationToken) is not null)
        {
            return;
        }

        // Segredo lido de configuração (env var / user-secrets em dev), nunca hardcoded — em produção
        // vira um valor gerenciado (AWS Secrets Manager/KMS), trocado sem mudar código (Fase 7).
        var clientSecret = configuration["Identity:Clients:BackendClient:Secret"]
            ?? throw new InvalidOperationException(
                "Configuração 'Identity:Clients:BackendClient:Secret' não definida.");

        // Representa qualquer chamador de backend confiável (hoje: quem for testar/demonstrar a Api
        // de Accounts; nas próximas fases, Payments/Gateway chamando Accounts internamente) — não é
        // "a própria Accounts se autenticando", e sim o cliente M2M que carrega o escopo dela.
        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = "PixHub Backend Client (M2M)",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + AccountsApiScope
            }
        }, cancellationToken);
    }
}
