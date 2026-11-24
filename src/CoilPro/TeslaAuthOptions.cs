namespace CoilPro;

/// <summary>Configuration for the Tesla Fleet API OAuth flows.</summary>
public sealed class TeslaAuthOptions
{
    /// <summary>OAuth client id of the registered Tesla application.</summary>
    public required string ClientId { get; set; }

    /// <summary>Client secret. Required for confidential clients and the client-credentials flow.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Redirect URI. Required for the authorization-code flow.</summary>
    public Uri? RedirectUri { get; set; }

    /// <summary>Deployment region. Drives the default <see cref="Endpoints"/>.</summary>
    public TeslaRegion Region { get; set; } = TeslaRegion.Europe;

    /// <summary>Requested scopes.</summary>
    public TeslaScopes Scopes { get; set; } = TeslaScopes.Openid | TeslaScopes.OfflineAccess;

    /// <summary>Optional explicit endpoints. When null, derived from <see cref="Region"/>.</summary>
    public TeslaEndpoints? Endpoints { get; set; }
}
