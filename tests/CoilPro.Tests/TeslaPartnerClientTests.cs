using System.Net;
using System.Text.Json;
using CoilPro.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CoilPro.Tests;

/// <summary>
/// Covers the partner-account endpoints: request shape (path, method, body, query), the partner-token
/// caching that keeps registration from re-authenticating per call, response parsing, and the
/// domain validation that turns a pasted URL into an actionable error instead of a server-side reject.
/// </summary>
public class TeslaPartnerClientTests
{
    private const string FleetApiBaseUrl = "https://fleet-api.prd.eu.vn.cloud.tesla.com";

    private static TeslaPartnerClient Create(
        HttpMessageHandler handler,
        ITeslaAuthClient? authClient = null,
        TimeProvider? timeProvider = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(FleetApiBaseUrl) };
        return new TeslaPartnerClient(
            http,
            authClient ?? new FakePartnerAuthClient(),
            new CoilProClientOptions { BaseRetryDelay = TimeSpan.Zero },
            timeProvider);
    }

    [Fact]
    public async Task RegisterAsync_posts_the_domain_to_partner_accounts()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            """{"response":{"domain":"coil.example.com","account_id":"acc-1"}}""");
        var client = Create(handler);

        await client.RegisterAsync("coil.example.com");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"{FleetApiBaseUrl}/api/1/partner_accounts", request.Uri.ToString());

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("coil.example.com", body.RootElement.GetProperty("domain").GetString());
    }

    [Fact]
    public async Task RegisterAsync_authenticates_with_a_partner_token()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"domain":"coil.example.com"}}""");
        var authClient = new FakePartnerAuthClient();
        var client = Create(handler, authClient);

        await client.RegisterAsync("coil.example.com");

        Assert.Equal(1, authClient.PartnerTokenCallCount);
        Assert.Equal("Bearer partner-token-1", handler.LastRequest!.AuthorizationHeader);
    }

    [Fact]
    public async Task RegisterAsync_parses_the_account_payload()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            """
            {"response":{
              "account_id":"acc-42",
              "domain":"coil.example.com",
              "name":"CoilPro",
              "description":"Unofficial SDK",
              "client_id":"client-7",
              "created_at":"2026-07-28T10:00:00Z",
              "enterprise_tier":"free"
            }}
            """);
        var client = Create(handler);

        var account = await client.RegisterAsync("coil.example.com");

        Assert.Equal("acc-42", account.AccountId);
        Assert.Equal("coil.example.com", account.Domain);
        Assert.Equal("CoilPro", account.Name);
        Assert.Equal("client-7", account.ClientId);
        Assert.Equal(DateTimeOffset.Parse("2026-07-28T10:00:00Z"), account.CreatedAt);

        // Unmodeled fields survive for forward-compatibility.
        Assert.NotNull(account.AdditionalData);
        Assert.Equal("free", account.AdditionalData!["enterprise_tier"].GetString());
    }

    [Fact]
    public async Task RegisterAsync_normalizes_the_domain_before_sending()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"domain":"coil.example.com"}}""");
        var client = Create(handler);

        await client.RegisterAsync("  Coil.Example.COM/  ");

        using var body = JsonDocument.Parse(handler.LastRequest!.Body!);
        Assert.Equal("coil.example.com", body.RootElement.GetProperty("domain").GetString());
    }

    [Fact]
    public async Task RegisterAsync_surfaces_tesla_errors_with_their_error_code()
    {
        // The shape Tesla actually returns, including the txid we don't model.
        var handler = StubHttpMessageHandler.ReturningJson(
            """{"response":null,"error":"could not fetch public key","error_description":"","txid":"5c469e62"}""",
            HttpStatusCode.BadRequest);
        var client = Create(handler);

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.RegisterAsync("coil.example.com"));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("could not fetch public key", ex.ErrorCode);
    }

    [Fact]
    public async Task GetPublicKeyAsync_sends_the_domain_as_a_query_parameter()
    {
        // A body instead of a query parameter yields a 404 from Tesla, so this is worth pinning.
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"public_key":"04abc"}}""");
        var client = Create(handler);

        await client.GetPublicKeyAsync("coil.example.com");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/1/partner_accounts/public_key", request.Uri.AbsolutePath);
        Assert.Equal("coil.example.com", request.QueryValues["domain"]);
        Assert.Null(request.Body);
    }

    [Fact]
    public async Task GetPublicKeyAsync_returns_the_registered_key()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"public_key":"04deadbeef"}}""");
        var client = Create(handler);

        Assert.Equal("04deadbeef", await client.GetPublicKeyAsync("coil.example.com"));
    }

    [Fact]
    public async Task GetPublicKeyAsync_returns_null_when_no_key_is_registered()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"public_key":""}}""");
        var client = Create(handler);

        Assert.Null(await client.GetPublicKeyAsync("coil.example.com"));
    }

    [Fact]
    public async Task Partner_token_is_reused_across_calls_while_it_is_valid()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse("""{"response":{"public_key":"04abc"}}"""));
        var authClient = new FakePartnerAuthClient();
        var client = Create(handler, authClient);

        await client.GetPublicKeyAsync("coil.example.com");
        await client.GetPublicKeyAsync("coil.example.com");
        await client.GetPublicKeyAsync("coil.example.com");

        Assert.Equal(1, authClient.PartnerTokenCallCount);
    }

    [Fact]
    public async Task Partner_token_is_refetched_once_it_is_close_to_expiry()
    {
        var now = new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(now);

        // Expires in 10s — inside the 30s safety margin, so it must not be reused.
        var authClient = new FakePartnerAuthClient(call => new TeslaTokens
        {
            AccessToken = $"partner-token-{call}",
            ExpiresAt = now.AddSeconds(10),
        });

        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse("""{"response":{"public_key":"04abc"}}"""));
        var client = Create(handler, authClient, time);

        await client.GetPublicKeyAsync("coil.example.com");
        await client.GetPublicKeyAsync("coil.example.com");

        Assert.Equal(2, authClient.PartnerTokenCallCount);
        Assert.Equal("Bearer partner-token-2", handler.LastRequest!.AuthorizationHeader);
    }

    [Fact]
    public async Task Rate_limit_headers_are_exposed_after_a_call()
    {
        var handler = StubHttpMessageHandler.ReturningSequence(
            StubHttpMessageHandler.JsonResponse(
                """{"response":{"domain":"coil.example.com"}}""",
                HttpStatusCode.OK,
                [new("x-ratelimit-limit", "500"), new("x-ratelimit-remaining", "499")]));
        var client = Create(handler);

        Assert.Null(client.LastRateLimit);

        await client.RegisterAsync("coil.example.com");

        Assert.Equal(500, client.LastRateLimit!.Limit);
        Assert.Equal(499, client.LastRateLimit.Remaining);
    }

    [Fact]
    public async Task Transient_failures_are_retried()
    {
        var handler = StubHttpMessageHandler.ReturningSequence(
            StubHttpMessageHandler.JsonResponse("""{"error":"upstream"}""", HttpStatusCode.ServiceUnavailable),
            StubHttpMessageHandler.JsonResponse("""{"response":{"domain":"coil.example.com"}}"""));
        var client = Create(handler);

        var account = await client.RegisterAsync("coil.example.com");

        Assert.Equal("coil.example.com", account.Domain);
        Assert.Equal(2, handler.Requests.Count);

        // Each retry must carry its own body — a consumed HttpContent cannot be re-sent.
        Assert.All(handler.Requests, r => Assert.Contains("coil.example.com", r.Body));
    }

    [Theory]
    [InlineData("https://coil.example.com")]
    [InlineData("http://coil.example.com")]
    [InlineData("coil.example.com/path")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeDomain_rejects_anything_that_is_not_a_bare_host(string domain)
    {
        Assert.Throws<ArgumentException>(() => TeslaPartnerClient.NormalizeDomain(domain));
    }

    [Theory]
    [InlineData("coil.example.com", "coil.example.com")]
    [InlineData("  Coil.Example.com  ", "coil.example.com")]
    [InlineData("coil.example.com/", "coil.example.com")]
    public void NormalizeDomain_trims_and_lowercases(string input, string expected)
    {
        Assert.Equal(expected, TeslaPartnerClient.NormalizeDomain(input));
    }
}
