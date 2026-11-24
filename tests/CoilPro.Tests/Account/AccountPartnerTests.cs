using System.Net;
using System.Text.Json;
using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests.Account;

public class AccountPartnerTests
{
    private const string Base = "https://fleet-api.prd.eu.vn.cloud.tesla.com";

    private static TeslaPartnerClient Create(HttpMessageHandler handler, int maxRetries = 3) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(Base) },
            new FakePartnerAuthClient(),
            new CoilProClientOptions { MaxRetries = maxRetries, BaseRetryDelay = TimeSpan.Zero });

    [Fact]
    public async Task GetVehicleSpecs_uses_partner_token_and_parses_spec_example()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"vin":"TEST00000000VIN01","batterySoH":"100%","batteryCapacityKwh":"82.3kWh","batterySoHTimestamp":"2024-01-28T00:00:00Z","make":"Tesla","model":"m3","modelYear":2025,"equipmentPrice":[{"code":"$APBS","name":"Autopilot","price":0},{"code":"$IPW4","name":"Interior","price":1200}],"trim":"Performance","numberOfSeats":5,"thirdRowSeatsCode":null,"totalPrice":60990,"productionDate":"2025-07-18T02:50:26Z","maxPowerInKw":"158 kW","batteryRangeInKm":"571 km"}}""");
        var client = Create(handler);

        var specs = await client.GetVehicleSpecsAsync("TEST00000000VIN01");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal($"{Base}/api/1/vehicles/TEST00000000VIN01/specs", req.Uri.ToString());
        Assert.Equal("Bearer partner-token-1", req.AuthorizationHeader);
        Assert.Equal("100%", specs.BatterySoH);
        Assert.Equal("82.3kWh", specs.BatteryCapacityKwh);
        Assert.Equal(new DateTimeOffset(2024, 1, 28, 0, 0, 0, TimeSpan.Zero), specs.BatterySoHTimestamp);
        Assert.Equal(2025, specs.ModelYear);
        Assert.Equal(2, specs.EquipmentPrice!.Count);
        Assert.Equal(1200m, specs.EquipmentPrice[1].Price);
        Assert.Null(specs.ThirdRowSeatsCode);
        Assert.Equal(60990m, specs.TotalPrice);
    }

    [Fact]
    public async Task GetVehiclePricing_posts_body_and_parses_spec_example()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"configs":[{"variant":{"code":"AWD_P3","name":"All-Wheel Drive (P3)"},"currency_code":"USD","currency_symbol":"$","subtotal":109990,"orderFee":250,"deliveryFee":0,"vat":false,"options":[{"code":"$MDLS","name":"Model S","group":"MODEL","price":70000}],"warranty":{"isTaxInclusive":false,"options":[{"code":"$NW60","name":"New MFG Warranty","description":"d","group":"NEW_MFG_WARRANTY"}]},"specs":{"name":"Model S","range_formatted":"410 miles","topspeed_formatted":"130 mph","acceleration_formatted":"3.1 seconds","seating_capacity_adults":5}}],"accessories":[{"name":"Mobile Charger","price":300}]}}""");
        var client = Create(handler);

        var pricing = await client.GetVehiclePricingAsync("DE", "my", "de");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"{Base}/api/1/dx/vehicles/pricing", req.Uri.ToString());
        using var body = JsonDocument.Parse(req.Body!);
        Assert.Equal("DE", body.RootElement.GetProperty("market").GetString());
        Assert.Equal("my", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("de", body.RootElement.GetProperty("language").GetString());
        var cfg = Assert.Single(pricing.Configs!);
        Assert.Equal("AWD_P3", cfg.Variant!.Code);
        Assert.Equal(109990m, cfg.Subtotal);
        Assert.Equal(250m, cfg.OrderFee);
        Assert.False(cfg.Vat);
        Assert.Equal("MODEL", cfg.Options![0].Group);
        Assert.Equal("$NW60", cfg.Warranty!.Options![0].Code);
        Assert.Equal(5, cfg.Specs!.SeatingCapacityAdults);
        Assert.Equal(300m, pricing.Accessories![0].Price);
    }

    [Fact]
    public async Task GetVehiclePricing_retries_transient_failure_because_it_is_a_read()
    {
        var handler = StubHttpMessageHandler.ReturningSequence(
            StubHttpMessageHandler.JsonResponse("""{"error":"x"}""", HttpStatusCode.ServiceUnavailable),
            StubHttpMessageHandler.JsonResponse("""{"response":{"configs":[],"accessories":[]}}"""));
        var client = Create(handler);

        var pricing = await client.GetVehiclePricingAsync("DE", "my", "de");

        Assert.Empty(pricing.Configs!);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetFleetTelemetryErrors_unwraps_list()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"fleet_telemetry_errors":[{"name":"client-id","error":"msg","vin":"vin"},{"name":"client-id","error":"msg2","vin":"vin"}]}}""");
        var client = Create(handler);

        var errors = await client.GetFleetTelemetryErrorsAsync();

        Assert.Equal($"{Base}/api/1/partner_accounts/fleet_telemetry_errors", handler.LastRequest!.Uri.ToString());
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public async Task GetFleetTelemetryErrorVins_unwraps_list()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"fleet_telemetry_error_vins":["vin1","vin2"]}}""");
        var client = Create(handler);

        var vins = await client.GetFleetTelemetryErrorVinsAsync();

        Assert.Equal($"{Base}/api/1/partner_accounts/fleet_telemetry_error_vins", handler.LastRequest!.Uri.ToString());
        Assert.Equal(["vin1", "vin2"], vins);
    }
}
