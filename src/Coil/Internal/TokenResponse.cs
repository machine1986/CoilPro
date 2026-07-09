using System.Text.Json.Serialization;

namespace Coil.Internal;

/// <summary>Raw JSON shape of the Tesla Fleet API OAuth token endpoint response.</summary>
internal sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; init; }
}
