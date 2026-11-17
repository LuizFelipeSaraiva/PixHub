using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PixHub.Accounts.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace PixHub.Accounts.Api.Tests;

/// <summary>
/// Sobe um Postgres real (Testcontainers), aplica as migrations e hospeda a <c>PixHub.Accounts.Api</c>
/// completa via <see cref="WebApplicationFactory{TEntryPoint}"/> — os testes exercitam o pipeline real
/// de autenticação/autorização (JWT Bearer + <c>ScopeAuthorizationHandler</c>), não um mock de
/// middleware.
/// </summary>
/// <remarks>
/// Em produção, o Accounts.Api valida a assinatura do JWT descobrindo as chaves do Identity via
/// <c>Authority</c> (metadata OIDC). Repetir essa descoberta aqui acoplaria este teste ao Identity.Api
/// de pé — o que a suíte de Identity já cobre de ponta a ponta. Em vez disso, troca-se a validação por
/// uma chave simétrica conhecida só do teste (<see cref="IssueToken"/> assina com a mesma chave que o
/// host de teste usa para validar), mantendo o pipeline real de autorização por escopo sob teste.
/// </remarks>
public sealed class AccountsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Audience = "accounts-api";
    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("chave-simetrica-de-teste-nao-usar-em-producao-32+"));

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pixhub_accounts_api_tests")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__AccountsDb", _container.GetConnectionString());

        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql(_container.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "accounts"))
            .Options;

        await using var dbContext = new AccountsDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Substitui a descoberta de chaves via Authority por uma chave simétrica fixa,
                // conhecida só deste teste — ver remarks da classe. "Identity:Authority" não existe
                // no appsettings do Accounts.Api, então Authority/MetadataAddress já chegam nulos
                // aqui; só falta trocar a validação para usar a chave simétrica de teste.
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey,
                };
            }));
    }

    /// <summary>Emite um JWT assinado com a chave de teste, concedendo os escopos informados (claim <c>scope</c>, separados por espaço — RFC 6749).</summary>
    public static string IssueToken(params string[] scopes)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);

        var token = handler.CreateJwtSecurityToken(
            issuer: "https://identity.pixhub.test/",
            audience: Audience,
            subject: new ClaimsIdentity([new Claim("sub", "test-client"), new Claim("scope", string.Join(' ', scopes))]),
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            issuedAt: DateTime.UtcNow,
            signingCredentials: credentials);

        return handler.WriteToken(token);
    }
}
