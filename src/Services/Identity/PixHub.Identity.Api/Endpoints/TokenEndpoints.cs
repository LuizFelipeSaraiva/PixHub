using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PixHub.BuildingBlocks.Api;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PixHub.Identity.Api.Endpoints;

/// <summary>
/// Endpoint de token do servidor de autorização. O OpenIddict já validou client_id/client_secret e
/// os escopos pedidos contra as permissões do client (registradas pelo <c>ClientsSeeder</c>) antes de
/// chegar aqui — o handler só decide *quem* o token representa e monta a identidade correspondente.
/// Só <c>client_credentials</c> está implementado: não há usuário nesse fluxo, então a "identidade" é
/// a própria aplicação cliente.
/// </summary>
internal static class TokenEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/connect/token", ExchangeAsync)
            .RequireRateLimiting(RateLimitingExtensions.TokenEndpointPolicyName);

        return app;
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext httpContext,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        CancellationToken cancellationToken)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("A requisição OpenIddict não pôde ser recuperada.");

        if (!request.IsClientCredentialsGrantType())
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
            ?? throw new InvalidOperationException("Aplicação cliente não encontrada.");

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

        // "sub" é o client_id: em client_credentials o token representa a aplicação, não uma pessoa.
        identity.SetClaim(Claims.Subject, await applicationManager.GetClientIdAsync(application, cancellationToken));
        identity.SetClaim(Claims.Name, await applicationManager.GetDisplayNameAsync(application, cancellationToken));

        // Só concede os escopos que o client pediu E tem permissão — o OpenIddict já filtrou pela
        // interseção antes de chegar aqui; devolvemos exatamente o que foi validado. O audience
        // ("resource") do token vem dos escopos concedidos, não do client — é o escopo que sabe a
        // qual Api ele dá acesso (ex.: "accounts.api" -> resource "accounts-api").
        var scopes = request.GetScopes();
        identity.SetScopes(scopes);

        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(scopes, cancellationToken))
        {
            resources.Add(resource);
        }

        identity.SetResources(resources);

        // Todas essas claims vão para o access token (não há id_token nem authorization code aqui).
        identity.SetDestinations(static _ => [Destinations.AccessToken]);

        return Results.SignIn(
            new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
