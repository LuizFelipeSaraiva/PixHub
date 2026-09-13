using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PixHub.Accounts.Api.Endpoints;
using PixHub.Accounts.Application;
using PixHub.Accounts.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();

// Exceções não tratadas viram ProblemDetails (RFC 9457) em vez de vazar stack trace — falhas
// esperadas de negócio já são tratadas via Result/ResultExtensions e nunca chegam aqui. Com
// IProblemDetailsService registrado, UseExceptionHandler() sem parâmetros já produz esse formato.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapAccountEndpoints();

// Liveness não roda nenhum health check (só confirma que o processo responde); readiness roda os
// checks marcados com a tag "ready" (hoje, só a conectividade com o Postgres).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

// Torna o host visível para WebApplicationFactory<Program> nos testes de integração.
namespace PixHub.Accounts.Api
{
    public partial class Program;
}
