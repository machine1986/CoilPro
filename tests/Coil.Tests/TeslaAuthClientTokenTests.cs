using System.Net;
using Coil.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace Coil.Tests;

public class TeslaAuthClientTokenTests
{
    private const string TokenJson = """
        {
          "access_token": "access-123",
          "refresh_token": "refresh-456",
          "id_token": "id-789",
          "token_type": "Bearer",
          "expires_in": 3600
        }
        """;

    private static TeslaAuthOptions CreateOptions(string? clientSecret = null) => new()
    {
        ClientId = "test-client-id",
        ClientSecret = clientSecret,
        RedirectUri = new Uri("https://app.example.com/callback"),
        Region = TeslaRegion.NorthAmerica,
        Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess,
    };

    [Fact]
    public async Task ExchangeCodeAsync_ParsesTokenResponse_AndSendsExpectedFormBody()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var options = CreateOptions();
        var client = new TeslaAuthClient(new HttpClient(handler), options, timeProvider);

        var tokens = await client.ExchangeCodeAsync("auth-code", "verifier-value");

        Assert.Equal("access-123", tokens.AccessToken);
        Assert.Equal("refresh-456", tokens.RefreshToken);
        Assert.Equal("id-789", tokens.IdToken);
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.Equal(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(3600), tokens.ExpiresAt);

        var captured = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Contains("token", captured.Uri.AbsolutePath);

        var form = captured.FormValues;
        Assert.Equal("authorization_code", form["grant_type"]);
        Assert.Equal(options.ClientId, form["client_id"]);
        Assert.Equal("auth-code", form["code"]);
        Assert.Equal("verifier-value", form["code_verifier"]);
        Assert.Equal(options.RedirectUri!.ToString(), form["redirect_uri"]);
        Assert.False(form.ContainsKey("client_secret"));
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithClientSecretConfigured_IncludesItInFormBody()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var options = CreateOptions(clientSecret: "s3cr3t");
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        await client.ExchangeCodeAsync("auth-code", "verifier-value");

        var form = Assert.Single(handler.Requests).FormValues;
        Assert.Equal("s3cr3t", form["client_secret"]);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenRedirectUriIsNull_Throws()
    {
        var options = new TeslaAuthOptions
        {
            ClientId = "test-client-id",
            RedirectUri = null,
            Region = TeslaRegion.Europe,
        };
        var client = new TeslaAuthClient(
            new HttpClient(new StubHttpMessageHandler(_ =>
                throw new InvalidOperationException("Should not call HTTP."))),
            options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ExchangeCodeAsync("auth-code", "verifier-value"));
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenTokenEndpointReturnsError_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"invalid_grant"}"""),
        });
        var options = CreateOptions();
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.ExchangeCodeAsync("bad-code", "verifier-value"));
    }

    [Fact]
    public async Task RefreshAsync_SendsRefreshTokenGrant_AndParsesTokens()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var options = CreateOptions();
        var client = new TeslaAuthClient(new HttpClient(handler), options, timeProvider);

        var tokens = await client.RefreshAsync("old-refresh-token");

        Assert.Equal("access-123", tokens.AccessToken);
        Assert.Equal("refresh-456", tokens.RefreshToken);
        Assert.Equal(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(3600), tokens.ExpiresAt);

        var form = Assert.Single(handler.Requests).FormValues;
        Assert.Equal("refresh_token", form["grant_type"]);
        Assert.Equal(options.ClientId, form["client_id"]);
        Assert.Equal("old-refresh-token", form["refresh_token"]);
    }

    [Fact]
    public async Task GetPartnerTokenAsync_SendsClientCredentialsGrant_WithScopeAndAudience()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var options = CreateOptions(clientSecret: "s3cr3t");
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        var tokens = await client.GetPartnerTokenAsync();

        Assert.Equal("access-123", tokens.AccessToken);

        var form = Assert.Single(handler.Requests).FormValues;
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal(options.ClientId, form["client_id"]);
        Assert.Equal("s3cr3t", form["client_secret"]);
        Assert.Equal(options.Scopes.ToScopeString(), form["scope"]);
        // Literal on purpose: comparing against FleetApiBaseUrl.ToString() hid a trailing "/" that the
        // live token endpoint rejects with 400 invalid_audience.
        Assert.Equal("https://fleet-api.prd.na.vn.cloud.tesla.com", form["audience"]);
    }

    [Theory]
    [InlineData(TeslaRegion.NorthAmerica, "https://fleet-api.prd.na.vn.cloud.tesla.com")]
    [InlineData(TeslaRegion.Europe, "https://fleet-api.prd.eu.vn.cloud.tesla.com")]
    public async Task GetPartnerTokenAsync_SendsAudienceWithoutTrailingSlash(TeslaRegion region, string expectedAudience)
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var options = CreateOptions(clientSecret: "s3cr3t");
        options.Region = region;
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        await client.GetPartnerTokenAsync();

        Assert.Equal(expectedAudience, Assert.Single(handler.Requests).FormValues["audience"]);
    }

    [Fact]
    public async Task GetPartnerTokenAsync_WithoutClientSecretConfigured_Throws()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var options = CreateOptions(clientSecret: null);
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPartnerTokenAsync());
    }

    [Fact]
    public async Task ExchangeCodeAsync_ExpiresAt_IsWithinToleranceOfNowPlusExpiresIn_WhenUsingSystemClock()
    {
        var handler = StubHttpMessageHandler.ReturningJson(TokenJson);
        var options = CreateOptions();
        var client = new TeslaAuthClient(new HttpClient(handler), options);

        var before = DateTimeOffset.UtcNow;
        var tokens = await client.ExchangeCodeAsync("auth-code", "verifier-value");
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(tokens.ExpiresAt, before.AddSeconds(3600 - 5), after.AddSeconds(3600 + 5));
    }
}
