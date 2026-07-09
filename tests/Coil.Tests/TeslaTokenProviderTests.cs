using Coil.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace Coil.Tests;

public class TeslaTokenProviderTests
{
    private static TeslaTokens TokensExpiringIn(DateTimeOffset now, TimeSpan validFor, string? refreshToken = "refresh-1") => new()
    {
        AccessToken = "access-1",
        RefreshToken = refreshToken,
        ExpiresAt = now + validFor,
    };

    [Fact]
    public async Task GetAccessTokenAsync_WithNonExpiredToken_ReturnsItAsIs_WithoutRefreshing()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = new InMemoryTokenStore();
        await store.SaveAsync(TokensExpiringIn(timeProvider.GetUtcNow(), TimeSpan.FromHours(1)));
        var authClient = new FakeTeslaAuthClient(_ => throw new InvalidOperationException("Should not refresh."));
        var provider = new TeslaTokenProvider(store, authClient, timeProvider);

        var token = await provider.GetAccessTokenAsync();

        Assert.Equal("access-1", token);
        Assert.Equal(0, authClient.RefreshCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithExpiredTokenAndRefreshToken_RefreshesPersistsAndReturnsNewAccessToken()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = new InMemoryTokenStore();
        await store.SaveAsync(TokensExpiringIn(timeProvider.GetUtcNow(), TimeSpan.FromMinutes(-5), "refresh-1"));

        var newTokens = new TeslaTokens
        {
            AccessToken = "access-2",
            RefreshToken = "refresh-2",
            ExpiresAt = timeProvider.GetUtcNow() + TimeSpan.FromHours(1),
        };
        var authClient = new FakeTeslaAuthClient(refreshToken =>
        {
            Assert.Equal("refresh-1", refreshToken);
            return newTokens;
        });
        var provider = new TeslaTokenProvider(store, authClient, timeProvider);

        var token = await provider.GetAccessTokenAsync();

        Assert.Equal("access-2", token);
        Assert.Equal(1, authClient.RefreshCallCount);
        Assert.Equal("refresh-1", authClient.LastRefreshToken);

        var persisted = await store.GetAsync();
        Assert.Equal("access-2", persisted!.AccessToken);
        Assert.Equal("refresh-2", persisted.RefreshToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithTokenExpiringWithinRefreshLeeway_RefreshesEarlyRatherThanRacingTheClock()
    {
        // The provider refreshes a little before actual expiry (documented "leeway"); an access
        // token that is technically still valid but expiring imminently should still trigger refresh.
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = new InMemoryTokenStore();
        await store.SaveAsync(TokensExpiringIn(timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), "refresh-1"));

        var newTokens = new TeslaTokens
        {
            AccessToken = "access-2",
            RefreshToken = "refresh-2",
            ExpiresAt = timeProvider.GetUtcNow() + TimeSpan.FromHours(1),
        };
        var authClient = new FakeTeslaAuthClient(_ => newTokens);
        var provider = new TeslaTokenProvider(store, authClient, timeProvider);

        var token = await provider.GetAccessTokenAsync();

        Assert.Equal("access-2", token);
        Assert.Equal(1, authClient.RefreshCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithExpiredTokenAndNoRefreshToken_ThrowsInvalidOperationException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = new InMemoryTokenStore();
        await store.SaveAsync(TokensExpiringIn(timeProvider.GetUtcNow(), TimeSpan.FromMinutes(-5), refreshToken: null));
        var authClient = new FakeTeslaAuthClient(_ => throw new InvalidOperationException("Should not be called — no refresh token."));
        var provider = new TeslaTokenProvider(store, authClient, timeProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAccessTokenAsync().AsTask());
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithNoStoredTokensAtAll_ThrowsInvalidOperationException()
    {
        var store = new InMemoryTokenStore();
        var authClient = new FakeTeslaAuthClient(_ => throw new InvalidOperationException("Should not be called — nothing stored."));
        var provider = new TeslaTokenProvider(store, authClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAccessTokenAsync().AsTask());
    }
}
