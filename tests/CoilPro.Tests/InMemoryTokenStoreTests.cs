namespace CoilPro.Tests;

public class InMemoryTokenStoreTests
{
    private static TeslaTokens CreateTokens() => new()
    {
        AccessToken = "access-123",
        RefreshToken = "refresh-456",
        IdToken = "id-789",
        TokenType = "Bearer",
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
    };

    [Fact]
    public async Task GetAsync_WhenNothingSaved_ReturnsNull()
    {
        var store = new InMemoryTokenStore();

        var result = await store.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsSameTokens()
    {
        var store = new InMemoryTokenStore();
        var tokens = CreateTokens();

        await store.SaveAsync(tokens);
        var result = await store.GetAsync();

        Assert.Equal(tokens, result);
    }

    [Fact]
    public async Task SaveAsync_Twice_ReplacesPreviouslyStoredTokens()
    {
        var store = new InMemoryTokenStore();
        await store.SaveAsync(CreateTokens());
        var replacement = CreateTokens() with { AccessToken = "access-999" };

        await store.SaveAsync(replacement);
        var result = await store.GetAsync();

        Assert.Equal("access-999", result!.AccessToken);
    }

    [Fact]
    public async Task ClearAsync_AfterSave_RemovesStoredTokens()
    {
        var store = new InMemoryTokenStore();
        await store.SaveAsync(CreateTokens());

        await store.ClearAsync();
        var result = await store.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task RoundTrip_SaveThenGetThenClearThenGet_BehavesAsExpected()
    {
        var store = new InMemoryTokenStore();
        var tokens = CreateTokens();

        await store.SaveAsync(tokens);
        Assert.Equal(tokens, await store.GetAsync());

        await store.ClearAsync();
        Assert.Null(await store.GetAsync());
    }
}
