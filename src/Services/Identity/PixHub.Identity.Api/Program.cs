using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PixHub.BuildingBlocks.Api;
using PixHub.Identity.Api.Endpoints;
using PixHub.Identity.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowClientCredentialsFlow();
        options.RegisterScopes(ClientsSeeder.AccountsApiScope);

        // Tokens JWT simples (não criptografados) — mais fácil de inspecionar/depurar e de validar
        // de outro serviço via JWKS padrão. Certificados de dev são gerados e cacheados localmente
        // pelo próprio OpenIddict; em produção, viram certificados geridos via KMS/Secrets Manager
        // (Fase 7), sem mudar este código.
        options.DisableAccessTokenEncryption();
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();

        var aspNetCore = options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();

        // Em produção, o servidor de autorização só deve aceitar HTTPS (o segredo do client viaja no
        // corpo da requisição) — a exigência só é relaxada em dev/teste local, onde não há TLS na
        // frente e o tráfego nunca sai da máquina.
        if (builder.Environment.IsDevelopment())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    });

builder.Services.AddOpenApi();
builder.Services.AddDefaultRateLimiting();

// Exceções não tratadas viram ProblemDetails (RFC 9457) em vez de vazar stack trace.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSecurityHeaders();
app.UseRateLimiter();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapTokenEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

// Torna o host visível para WebApplicationFactory<Program> nos testes de integração.
namespace PixHub.Identity.Api
{
    public partial class Program;
}
