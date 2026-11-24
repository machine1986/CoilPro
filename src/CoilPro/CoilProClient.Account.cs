using System.Globalization;
using System.Text.Json;
using CoilPro.Internal;

namespace CoilPro;

public sealed partial class CoilProClient
{
    private static readonly JsonSerializerOptions AccountJsonOptions = new();

    // ---- Unsigned REST commands ----------------------------------------------------------------

    /// <inheritdoc />
    public Task<RestCommandResult> ShareNavigationDestinationAsync(string vin, string destination, string? locale = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        // timestamp_ms is a string per the Tesla spec.
        var body = new Dictionary<string, object?>
        {
            ["type"] = "share_ext_content_raw",
            ["value"] = new Dictionary<string, string> { ["android.intent.extra.TEXT"] = destination },
            ["locale"] = string.IsNullOrWhiteSpace(locale) ? "en-US" : locale,
            ["timestamp_ms"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        };
        return SendRestCommandAsync(vin, "navigation_request", body, ct);
    }

    /// <inheritdoc />
    public Task<RestCommandResult> NavigateToCoordinatesAsync(string vin, double latitude, double longitude, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) =>
        SendRestCommandAsync(vin, "navigation_gps_request", new Dictionary<string, object?>
        {
            ["lat"] = latitude,
            ["lon"] = longitude,
            ["order"] = (int)order,
        }, ct);

    /// <inheritdoc />
    public Task<RestCommandResult> NavigateToSuperchargerAsync(string vin, long superchargerId, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) =>
        SendRestCommandAsync(vin, "navigation_sc_request", new Dictionary<string, object?>
        {
            ["id"] = superchargerId,
            ["order"] = (int)order,
        }, ct);

    /// <inheritdoc />
    public Task<RestCommandResult> SetNavigationWaypointsAsync(string vin, IEnumerable<string> placeIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(placeIds);
        var ids = placeIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => $"refId:{id.Trim()}").ToList();
        if (ids.Count == 0)
        {
            throw new ArgumentException("At least one place id is required.", nameof(placeIds));
        }

        return SendRestCommandAsync(vin, "navigation_waypoints_request", new Dictionary<string, object?>
        {
            ["waypoints"] = string.Join(",", ids),
        }, ct);
    }

    private async Task<RestCommandResult> SendRestCommandAsync(string vin, string command, object body, CancellationToken ct)
    {
        var path = $"api/1/vehicles/{Uri.EscapeDataString(vin)}/command/{command}";

        // Mutation: never retried, the first attempt may already have taken effect.
        var result = await _executor.SendAsync<RestCommandResult>(
            HttpMethod.Post, path, FleetApiExecutor.JsonBody(body), ct, retryable: false).ConfigureAwait(false);

        return result.Value ?? throw EmptyResponse(result.StatusCode, command);
    }

    // ---- Users ----------------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<UserProfile> GetMeAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<UserProfile>(HttpMethod.Get, "api/1/users/me", null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "users/me");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleOrder>> GetOrdersAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<List<VehicleOrder>>(HttpMethod.Get, "api/1/users/orders", null, ct).ConfigureAwait(false);
        return result.Value ?? [];
    }

    /// <inheritdoc />
    public async Task<UserRegion> GetRegionAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<UserRegion>(HttpMethod.Get, "api/1/users/region", null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "users/region");
    }

    // ---- Charging -------------------------------------------------------------------------------

    /// <inheritdoc />
    public Task<byte[]> GetChargingInvoiceAsync(string contentId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        return _executor.SendForBytesAsync($"api/1/dx/charging/invoice/{Uri.EscapeDataString(contentId)}", ct);
    }

    /// <inheritdoc />
    public async Task<ChargingSessionsPage> GetChargingSessionsAsync(ChargingSessionsQuery? query = null, CancellationToken ct = default)
    {
        var parameters = new List<string>();
        if (query is not null)
        {
            AddParam(parameters, "vin", query.Vin);
            AddParam(parameters, "date_from", query.DateFrom?.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture));
            AddParam(parameters, "date_to", query.DateTo?.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture));
            AddParam(parameters, "limit", query.Limit?.ToString(CultureInfo.InvariantCulture));
            AddParam(parameters, "offset", query.Offset?.ToString(CultureInfo.InvariantCulture));
        }

        var path = parameters.Count == 0 ? "api/1/dx/charging/sessions" : "api/1/dx/charging/sessions?" + string.Join("&", parameters);
        return await GetTolerantAsync<ChargingSessionsPage>(path, ct).ConfigureAwait(false) ?? new ChargingSessionsPage();
    }

    // ---- Vehicle management ---------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleOption>> GetVehicleOptionsAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<VehicleOptionsPayload>(HttpMethod.Get, VinQuery("api/1/dx/vehicles/options", vin), null, ct).ConfigureAwait(false);
        return result.Value?.Codes ?? [];
    }

    /// <inheritdoc />
    public async Task<WarrantyDetails> GetWarrantyDetailsAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<WarrantyDetails>(HttpMethod.Get, VinQuery("api/1/dx/warranty/details", vin), null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "dx/warranty/details");
    }

    /// <inheritdoc />
    public async Task<SubscriptionEligibility> GetSubscriptionEligibilityAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<SubscriptionEligibility>(HttpMethod.Get, VinQuery("api/1/dx/vehicles/subscriptions/eligibility", vin), null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "dx/vehicles/subscriptions/eligibility");
    }

    /// <inheritdoc />
    public async Task<UpgradeEligibility> GetUpgradeEligibilityAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<UpgradeEligibility>(HttpMethod.Get, VinQuery("api/1/dx/vehicles/upgrades/eligibility", vin), null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "dx/vehicles/upgrades/eligibility");
    }

    /// <inheritdoc />
    public async Task<EnterpriseRoles> GetEnterpriseRolesAsync(string vin, CancellationToken ct = default)
    {
        // The docs example shows no envelope (unverified), so accept both shapes.
        var path = $"api/1/dx/enterprise/v1/{Uri.EscapeDataString(vin)}/roles";
        return await GetTolerantAsync<EnterpriseRoles>(path, ct).ConfigureAwait(false) ?? new EnterpriseRoles();
    }

    /// <inheritdoc />
    public async Task SetEnterprisePayerAsync(string vin, EnterprisePayerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.AccountId) == string.IsNullOrEmpty(request.FederationId))
        {
            throw new ArgumentException("Set exactly one of AccountId or FederationId.", nameof(request));
        }

        var body = new Dictionary<string, string>
        {
            ["role"] = request.Role == EnterprisePayerRole.Charging ? "CHARGING" : "SUBSCRIPTION",
        };
        if (!string.IsNullOrEmpty(request.AccountId))
        {
            body["account_id"] = request.AccountId;
        }
        else
        {
            body["federation_id"] = request.FederationId!;
        }

        // 204 with an empty body on success; mutation, so never retried.
        await _executor.SendAsync<JsonElement>(
            HttpMethod.Post,
            $"api/1/dx/enterprise/v1/{Uri.EscapeDataString(vin)}/payer",
            FleetApiExecutor.JsonBody(body),
            ct,
            unwrapEnvelope: false,
            retryable: false).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> GetApiStatusAsync(CancellationToken ct = default)
    {
        try
        {
            // Host-root path, plain text body ("ok"), so read raw bytes instead of JSON.
            var bytes = await _executor.SendForBytesAsync("status", ct).ConfigureAwait(false);
            return string.Equals(System.Text.Encoding.UTF8.GetString(bytes).Trim().Trim('"'), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch (TeslaApiException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static string VinQuery(string path, string vin) => $"{path}?vin={Uri.EscapeDataString(vin)}";

    private static void AddParam(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private TeslaApiException EmptyResponse(System.Net.HttpStatusCode status, string what) =>
        new(status, $"Fleet API returned a success status with an empty {what} response.", responseBody: null, rateLimit: LastRateLimit);

    /// <summary>GET that unwraps a top-level <c>response</c> when present and otherwise parses the body bare.</summary>
    private async Task<T?> GetTolerantAsync<T>(string path, CancellationToken ct)
    {
        var result = await _executor.SendAsync<JsonElement>(HttpMethod.Get, path, null, ct, unwrapEnvelope: false).ConfigureAwait(false);
        var element = result.Value;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        if (element.TryGetProperty("response", out var inner))
        {
            if (inner.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            element = inner;
        }

        try
        {
            return element.Deserialize<T>(AccountJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new TeslaApiException(result.StatusCode, $"Failed to parse Fleet API response body: {ex.Message}", responseBody: element.GetRawText(), rateLimit: LastRateLimit);
        }
    }

    private sealed record VehicleOptionsPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("codes")]
        public List<VehicleOption>? Codes { get; init; }
    }
}
