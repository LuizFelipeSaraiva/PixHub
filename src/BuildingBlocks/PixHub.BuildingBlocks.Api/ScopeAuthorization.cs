using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace PixHub.BuildingBlocks.Api;

/// <summary>
/// Autorização por escopo OAuth2 — o claim <c>scope</c> emitido pelo Identity é uma string única
/// separada por espaço (ex.: <c>"accounts.api ledger.read"</c>, formato padrão do RFC 6749), então
/// não dá pra usar <see cref="AuthorizationPolicyBuilder.RequireClaim(string, string[])"/> (que exige
/// igualdade exata) — precisa checar se o escopo pedido está *contido* na string.
/// </summary>
public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst("scope")?.Value;
        var scopes = scopeClaim?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

        if (scopes.Contains(requirement.Scope, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class ScopeAuthorizationExtensions
{
    /// <summary>Registra o handler; chame uma vez na composition root antes de declarar políticas com <see cref="RequireScope"/>.</summary>
    public static IServiceCollection AddScopeAuthorization(this IServiceCollection services) =>
        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) =>
        builder.AddRequirements(new ScopeRequirement(scope));
}
