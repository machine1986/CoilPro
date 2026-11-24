using Coil.Tests.TestSupport;

namespace Coil.Tests;

public class CreateAuthorizationRequestTests
{
    private static TeslaAuthOptions CreateOptions(Uri? redirectUri) => new()
    {
        ClientId = "test-client-id",
        RedirectUri = redirectUri,
        Region = TeslaRegion.Europe,
        Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData,
    };

    // CreateAuthorizationRequest must be pure URL/PKCE construction — no network I/O.
    private static HttpClient CreateHttpClientThatMustNotBeCalled() =>
        new(new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("CreateAuthorizationRequest must not perform HTTP calls.")));

    [Fact]
    public void CreateAuthorizationRequest_PointsAtTeslaAuthorizeEndpoint_WithExpectedQueryParameters()
    {
        var redirectUri = new Uri("https://app.example.com/callback");
        var options = CreateOptions(redirectUri);
        var client = new TeslaAuthClient(CreateHttpClientThatMustNotBeCalled(), options);

        var request = client.CreateAuthorizationRequest();

        // Literal on purpose: the authorize page lives on auth.tesla.com, not on the fleet-auth token host.
        // Pointing it at fleet-auth failed live with "redirect_uri ... not registered for this client_id".
        var url = request.AuthorizationUrl;
        Assert.Equal("https", url.Scheme);
        Assert.Equal("auth.tesla.com", url.Host);
        Assert.Equal("/oauth2/v3/authorize", url.AbsolutePath);

        var query = UrlEncoded.Parse(url.Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal(options.ClientId, query["client_id"]);
        Assert.Equal(redirectUri.ToString(), query["redirect_uri"]);
        Assert.Equal(options.Scopes.ToScopeString(), query["scope"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(request.State, query["state"]);
        Assert.Equal(request.Pkce.Challenge, query["code_challenge"]);
    }

    [Fact]
    public void CreateAuthorizationRequest_WithoutExplicitState_GeneratesNonEmptyStateEchoedInUrl()
    {
        var options = CreateOptions(new Uri("https://app.example.com/callback"));
        var client = new TeslaAuthClient(CreateHttpClientThatMustNotBeCalled(), options);

        var request = client.CreateAuthorizationRequest();

        Assert.False(string.IsNullOrEmpty(request.State));
        var query = UrlEncoded.Parse(request.AuthorizationUrl.Query);
        Assert.Equal(request.State, query["state"]);
    }

    [Fact]
    public void CreateAuthorizationRequest_WithExplicitState_UsesItVerbatimInResultAndUrl()
    {
        var options = CreateOptions(new Uri("https://app.example.com/callback"));
        var client = new TeslaAuthClient(CreateHttpClientThatMustNotBeCalled(), options);

        var request = client.CreateAuthorizationRequest("my-explicit-state");

        Assert.Equal("my-explicit-state", request.State);
        var query = UrlEncoded.Parse(request.AuthorizationUrl.Query);
        Assert.Equal("my-explicit-state", query["state"]);
    }

    [Fact]
    public void CreateAuthorizationRequest_TwoCalls_ProduceDifferentStateAndPkce()
    {
        var options = CreateOptions(new Uri("https://app.example.com/callback"));
        var client = new TeslaAuthClient(CreateHttpClientThatMustNotBeCalled(), options);

        var first = client.CreateAuthorizationRequest();
        var second = client.CreateAuthorizationRequest();

        Assert.NotEqual(first.State, second.State);
        Assert.NotEqual(first.Pkce.Verifier, second.Pkce.Verifier);
    }

    [Fact]
    public void CreateAuthorizationRequest_WhenRedirectUriIsNull_Throws()
    {
        var options = CreateOptions(redirectUri: null);
        var client = new TeslaAuthClient(CreateHttpClientThatMustNotBeCalled(), options);

        Assert.Throws<InvalidOperationException>(() => client.CreateAuthorizationRequest());
    }
}
