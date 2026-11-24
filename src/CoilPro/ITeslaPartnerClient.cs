namespace CoilPro;

/// <summary>
/// Partner-account endpoints: the one-time registration every application from
/// developer.tesla.com must complete before it can call the Fleet API on behalf of users.
/// </summary>
/// <remarks>
/// <para>
/// Prerequisites, in order:
/// </para>
/// <list type="number">
///   <item><description>Generate a P-256 key pair (<see cref="TeslaKeyPair.Generate"/>).</description></item>
///   <item><description>Serve its public PEM at <c>https://&lt;domain&gt;/.well-known/appspecific/com.tesla.3p.public-key.pem</c>, publicly and without authentication — Tesla fetches it during registration.</description></item>
///   <item><description>Call <see cref="RegisterAsync"/> with that domain.</description></item>
/// </list>
/// <para>
/// The domain must match the root domain of an <c>allowed_origin</c> configured for the application
/// on developer.tesla.com (e.g. <c>coil.abc.com</c> is acceptable for an allowed origin of
/// <c>www.abc.com</c>).
/// </para>
/// <para>
/// <b>Registration is per region.</b> An account registered against the EU host is not registered
/// against the North America host; calls to an unregistered region fail with region errors even when
/// the region is otherwise correct. Register against every region you intend to serve.
/// </para>
/// </remarks>
public interface ITeslaPartnerClient
{
    /// <summary>Rate-limit snapshot from the most recent response, or null if no call has been made yet.</summary>
    RateLimitInfo? LastRateLimit { get; }

    /// <summary>
    /// Registers <paramref name="domain"/> for this application in the region this client points at
    /// (<c>POST /api/1/partner_accounts</c>). Idempotent: re-registering an already-registered domain
    /// succeeds and refreshes the key Tesla has on file, which is how you rotate keys.
    /// </summary>
    /// <param name="domain">
    /// The bare domain hosting the public key, without scheme or path (e.g. <c>coil.example.com</c>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException"><paramref name="domain"/> is empty or includes a scheme or path.</exception>
    /// <exception cref="TeslaApiException">
    /// Tesla rejected the registration — most often because the public key is not reachable at the
    /// well-known path, is not a P-256 key, or the domain does not match an allowed origin.
    /// </exception>
    Task<PartnerAccount> RegisterAsync(string domain, CancellationToken ct = default);

    /// <summary>
    /// Returns the public key Tesla currently has on file for <paramref name="domain"/> as a
    /// hex-encoded uncompressed EC point, or null if none is registered
    /// (<c>GET /api/1/partner_accounts/public_key</c>). Use it to confirm a registration took effect
    /// and that the hosted key still matches — compare against <see cref="TeslaKeyPair.ExportPublicKeyHex"/>.
    /// </summary>
    /// <param name="domain">The registered domain, without scheme or path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> GetPublicKeyAsync(string domain, CancellationToken ct = default);
}
