using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil;

/// <summary>
/// A registered partner account as returned by <c>POST /api/1/partner_accounts</c>.
/// </summary>
/// <remarks>
/// Only the fields that are stable and useful are modeled; everything else is preserved in
/// <see cref="AdditionalData"/> so a Tesla-side addition never breaks deserialization.
/// </remarks>
public sealed record PartnerAccount
{
    /// <summary>Tesla's identifier for the registered partner account.</summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; init; }

    /// <summary>The domain the public key is served from, as registered.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    /// <summary>Application name shown to drivers during authorization.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Application description shown to drivers during authorization.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The OAuth client id this registration belongs to.</summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    /// <summary>When the registration was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the registration was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Any fields not modeled above, preserved for forward-compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>
/// The public key Tesla has on file for a domain, as returned by
/// <c>GET /api/1/partner_accounts/public_key</c>.
/// </summary>
internal sealed record PartnerPublicKeyResponse
{
    /// <summary>Hex-encoded uncompressed EC point (<c>04…</c>), or null if no key is registered.</summary>
    [JsonPropertyName("public_key")]
    public string? PublicKey { get; init; }
}
