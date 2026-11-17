using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PixHub.Accounts.Api.Endpoints;
using PixHub.Accounts.Application;
using PixHub.Accounts.Infrastructure;
using PixHub.BuildingBlocks.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddDefaultRateLimiting();

// Recurso protegido: valida tokens emitidos pelo Identity (servidor de autorização OAuth2/OIDC
// separado — Fase 2). "accounts-api" é o audience do token, registrado do lado do Identity como
// Resource do escopo "accounts.api"; os dois nomes são o contrato entre os dois serviços, não código
// compartilhado (são deployáveis independentemente).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = "accounts-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddScopeAuthorization();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("accounts.api", policy => policy.RequireScope("accounts.api"));

// Exceções não tratadas viram ProblemDetails (RFC 9457) em vez de vazar stack trace — falhas
// esperadas de negócio já são tratadas via Result/ResultExtensions e nunca chegam aqui. Com
// IProblemDetailsService registrado, UseExceptionHandler() sem parâmetros já produz esse formato.
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

app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints()
    .RequireAuthorization("accounts.api")
    .RequireRateLimiting(RateLimitingExtensions.GlobalPolicyName);

// Liveness não roda nenhum health check (só confirma que o processo responde); readiness roda os
// checks marcados com a tag "ready" (hoje, só a conectividade com o Postgres). Ambos ficam fora da
// autenticação — orquestradores (Kubernetes/ECS) não carregam um token para sondar o processo.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous();

app.Run();

// Torna o host visível para WebApplicationFactory<Program> nos testes de integração.
namespace PixHub.Accounts.Api
{
    public partial class Program;
}
