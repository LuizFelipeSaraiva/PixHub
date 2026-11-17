using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace PixHub.BuildingBlocks.Api;

/// <summary>
/// Rate limiting via o middleware nativo do ASP.NET Core (<c>Microsoft.AspNetCore.RateLimiting</c>) —
/// sem pacote de terceiros. Duas políticas: <see cref="GlobalPolicyName"/> (aplicada a toda a Api,
/// por IP) e <see cref="TokenEndpointPolicyName"/> (mais restritiva, para endpoints de emissão de
/// token/credenciais, onde força bruta é o risco principal).
/// </summary>
public static class RateLimitingExtensions
{
    public const string GlobalPolicyName = "global";
    public const string TokenEndpointPolicyName = "token-endpoint";

    public static IServiceCollection AddDefaultRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partição por IP do cliente: um IP abusivo não consome a cota de outro.
            options.AddPolicy(GlobalPolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientAddress(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            options.AddPolicy(TokenEndpointPolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientAddress(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    private static string GetClientAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
