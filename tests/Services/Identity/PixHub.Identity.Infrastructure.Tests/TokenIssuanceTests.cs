using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace PixHub.Identity.Infrastructure.Tests;

/// <summary>
/// Exercita <c>POST /connect/token</c> de ponta a ponta: Api real (via WebApplicationFactory),
/// OpenIddict de verdade validando client/escopo, Postgres real guardando o client seedado.
/// </summary>
public sealed class TokenIssuanceTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    private const string ClientId = "pixhub-backend-client";

    [Fact]
    public async Task ExchangeAsync_ComCredenciaisValidas_DeveEmitirTokenComOEscopoConcedido()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateClient();

        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new("grant_type", "client_credentials"),
            new("client_id", ClientId),
            new("client_secret", IdentityApiFixture.BackendClientSecret),
            new("scope", "accounts.api")
        ]), cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);

        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        payload.GetProperty("token_type").GetString().ShouldBe("Bearer", body);

        var accessToken = payload.GetProperty("access_token").GetString();
        accessToken.ShouldNotBeNullOrWhiteSpace();
        // JWT compacto tem 3 segmentos (header.payload.signature) — confirma que não é um token
        // "de referência" opaco, o que bateria com DisableAccessTokenEncryption() no servidor.
        var segments = accessToken!.Split('.');
        segments.Length.ShouldBe(3);

        // O escopo concedido vai dentro do claim "scope" do JWT, não no envelope de resposta
        // do token endpoint (que só traz access_token/token_type/expires_in por padrão).
        var claims = JsonSerializer.Deserialize<JsonElement>(DecodeJwtSegment(segments[1]));
        claims.GetProperty("scope").GetString().ShouldBe("accounts.api", body);
    }

    [Fact]
    public async Task ExchangeAsync_ComSegredoInvalido_NaoDeveEmitirToken()
    {
        using var client = fixture.CreateClient();

        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new("grant_type", "client_credentials"),
            new("client_id", ClientId),
            new("client_secret", "segredo-errado"),
            new("scope", "accounts.api")
        ]), TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    private static string DecodeJwtSegment(string segment)
    {
        var base64 = segment.Replace('-', '+').Replace('_', '/');
        var padding = (4 - (base64.Length % 4)) % 4;
        base64 += new string('=', padding);
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
