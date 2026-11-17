using System.Net;
using System.Net.Http.Headers;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Api.Tests;

/// <summary>
/// Exercita o <c>/api/accounts</c> protegido de ponta a ponta — Api real (via
/// <c>WebApplicationFactory</c>), pipeline real de JWT Bearer + <c>ScopeAuthorizationHandler</c>.
/// </summary>
public sealed class ScopeAuthorizationTests(AccountsApiFixture fixture) : IClassFixture<AccountsApiFixture>
{
    [Fact]
    public async Task GetAccounts_SemToken_DeveRetornarNaoAutorizado()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync("/api/accounts", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_ComEscopoDiferente_DeveRetornarProibido()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccountsApiFixture.IssueToken("ledger.read"));

        using var response = await client.GetAsync("/api/accounts", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAccounts_ComEscopoCorreto_DevePassarPelaAutorizacao()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccountsApiFixture.IssueToken("accounts.api"));

        using var response = await client.GetAsync("/api/accounts", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }
}
