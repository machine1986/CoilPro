using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Text.Json;
using static CoilPro.Commands.Tests.Telemetry.ReferenceSchnorrVerifier;

namespace CoilPro.Commands.Tests.Telemetry;

public sealed class FleetTelemetryConfiguratorTests
{
    private const string Vin = "5YJ3E1EA7KF317000";

    private sealed class FixedTokenProvider : ITeslaTokenProvider
    {
        public ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default) => new("token-123");
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string Body);

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.ToString(),
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static FleetTelemetryConfig Config() => new()
    {
        Hostname = "telemetry.example.com",
        Ca = "-----BEGIN CERTIFICATE-----\nMIIB+/=\n-----END CERTIFICATE-----\n",
        Port = 4443,
        Fields = new Dictionary<string, FleetTelemetryFieldConfig>
        {
            ["BatteryLevel"] = new() { IntervalSeconds = 60 },
            ["VehicleSpeed"] = new() { IntervalSeconds = 10, MinimumDelta = 1.5 },
        },
        Expiry = 1_900_000_000,
    };

    private const string SuccessBody =
        """{"response":{"updated_vehicles":1,"skipped_vehicles":{"missing_key":[],"unsupported_hardware":[],"unsupported_firmware":[],"max_configs":[]}}}""";

    private static (FleetTelemetryConfigurator Configurator, StubHandler Handler) Create(TeslaKeyPair keyPair, HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fleet-api.prd.eu.vn.cloud.tesla.com/") };
        return (new FleetTelemetryConfigurator(http, new FixedTokenProvider(), keyPair), handler);
    }

    [Fact]
    public async Task ConfigureAsync_PostsSignedConfigAndParsesResult()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var (configurator, handler) = Create(keyPair, HttpStatusCode.OK, SuccessBody);

        var result = await configurator.ConfigureAsync([Vin], Config());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://fleet-api.prd.eu.vn.cloud.tesla.com/api/1/vehicles/fleet_telemetry_config_jws", request.Uri.ToString());
        Assert.Equal("Bearer token-123", request.Authorization);

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal([Vin], body.RootElement.GetProperty("vins").EnumerateArray().Select(v => v.GetString()));
        var token = body.RootElement.GetProperty("token").GetString()!;

        var parts = token.Split('.');
        var publicKey = keyPair.ExportPublicKeyUncompressedPoint();
        Assert.Equal(Result.Valid, Verify(publicKey, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Base64Url.DecodeFromChars(parts[2])));

        using var claims = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        var root = claims.RootElement;
        Assert.Equal("com.tesla.fleet.TelemetryClient", root.GetProperty("aud").GetString());
        Assert.Equal(Convert.ToBase64String(publicKey), root.GetProperty("iss").GetString());
        Assert.Equal("telemetry.example.com", root.GetProperty("hostname").GetString());
        Assert.Equal(Config().Ca, root.GetProperty("ca").GetString());
        Assert.Equal(4443, root.GetProperty("port").GetInt32());
        Assert.Equal(1_900_000_000, root.GetProperty("exp").GetInt64());
        Assert.Equal(60, root.GetProperty("fields").GetProperty("BatteryLevel").GetProperty("interval_seconds").GetInt32());
        Assert.False(root.GetProperty("fields").GetProperty("BatteryLevel").TryGetProperty("minimum_delta", out _)); // nulls omitted
        Assert.Equal(1.5, root.GetProperty("fields").GetProperty("VehicleSpeed").GetProperty("minimum_delta").GetDouble());
        Assert.False(root.TryGetProperty("alert_types", out _));
        Assert.False(root.TryGetProperty("delivery_policy", out _));

        // Typed properties or extension data — either way the response fields must survive.
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.Equal(1, parsed.RootElement.GetProperty("updated_vehicles").GetInt32());
        Assert.Equal(JsonValueKind.Array, parsed.RootElement.GetProperty("skipped_vehicles").GetProperty("missing_key").ValueKind);
    }

    [Fact]
    public async Task ConfigureAsync_MapsErrorToTeslaApiExceptionWithoutRetry()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var (configurator, handler) = Create(keyPair, HttpStatusCode.ServiceUnavailable, """{"response":null,"error":"upstream unavailable","error_description":""}""");

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => configurator.ConfigureAsync([Vin], Config()));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Equal("upstream unavailable", ex.ErrorCode);
        Assert.Contains("upstream unavailable", ex.ResponseBody);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ConfigureAsync_MissingEnvelopeThrows()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var (configurator, _) = Create(keyPair, HttpStatusCode.OK, """{"updated_vehicles":1}""");
        await Assert.ThrowsAsync<TeslaApiException>(() => configurator.ConfigureAsync([Vin], Config()));
    }

    [Fact]
    public async Task ConfigureAsync_ValidatesVins()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var (configurator, handler) = Create(keyPair, HttpStatusCode.OK, SuccessBody);

        await Assert.ThrowsAsync<ArgumentException>(() => configurator.ConfigureAsync([], Config()));
        await Assert.ThrowsAsync<ArgumentException>(() => configurator.ConfigureAsync([" "], Config()));
        await Assert.ThrowsAsync<ArgumentException>(() => configurator.ConfigureAsync(Enumerable.Repeat(Vin, 101), Config()));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void CreateToken_PassesAdditionalDataThroughAndOverwritesIssAud()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var config = Config() with
        {
            AdditionalData = new Dictionary<string, JsonElement>
            {
                ["prefer_typed"] = JsonSerializer.SerializeToElement(true),
                ["iss"] = JsonSerializer.SerializeToElement("attacker"),
                ["aud"] = JsonSerializer.SerializeToElement("attacker"),
            },
        };

        var token = FleetTelemetryConfigurator.CreateToken(config, keyPair);

        using var claims = JsonDocument.Parse(Base64Url.DecodeFromChars(token.Split('.')[1]));
        Assert.True(claims.RootElement.GetProperty("prefer_typed").GetBoolean());
        Assert.Equal("com.tesla.fleet.TelemetryClient", claims.RootElement.GetProperty("aud").GetString());
        Assert.Equal(Convert.ToBase64String(keyPair.ExportPublicKeyUncompressedPoint()), claims.RootElement.GetProperty("iss").GetString());
    }
}
