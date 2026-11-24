using System.Net;
using System.Text;
using System.Text.Json;
using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests.Account;

public class AccountClientTests
{
    private const string Vin = "5YJ30000000000001";
    private const string Base = CoilProClientFactory.FleetApiBaseUrl;

    private static (CoilProClient Client, StubHttpMessageHandler Handler) Create(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = StubHttpMessageHandler.ReturningJson(json, status);
        return (CoilProClientFactory.Create(handler), handler);
    }

    private static JsonElement Body(CapturedRequest r) => JsonDocument.Parse(r.Body!).RootElement;

    // ---- REST commands ----

    [Fact]
    public async Task ShareNavigationDestination_posts_share_intent_body()
    {
        var (client, handler) = Create("""{"response":{"result":true,"queued":false}}""");
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await client.ShareNavigationDestinationAsync(Vin, "Pariser Platz 1, Berlin");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"{Base}/api/1/vehicles/{Vin}/command/navigation_request", req.Uri.ToString());
        var body = Body(req);
        Assert.Equal("share_ext_content_raw", body.GetProperty("type").GetString());
        Assert.Equal("Pariser Platz 1, Berlin", body.GetProperty("value").GetProperty("android.intent.extra.TEXT").GetString());
        Assert.Equal("en-US", body.GetProperty("locale").GetString());
        Assert.Equal(JsonValueKind.String, body.GetProperty("timestamp_ms").ValueKind);
        Assert.True(long.Parse(body.GetProperty("timestamp_ms").GetString()!) >= before);
        Assert.True(result.Result);
        Assert.False(result.Queued);
    }

    [Fact]
    public async Task ShareNavigationDestination_uses_given_locale()
    {
        var (client, handler) = Create("""{"response":{"result":true,"reason":""}}""");
        await client.ShareNavigationDestinationAsync(Vin, "Berlin", "de-DE");
        Assert.Equal("de-DE", Body(handler.LastRequest!).GetProperty("locale").GetString());
    }

    [Fact]
    public async Task NavigateToCoordinates_posts_lat_lon_order()
    {
        var (client, handler) = Create("""{"response":{"result":true,"reason":""}}""");

        var result = await client.NavigateToCoordinatesAsync(Vin, 52.5, 13.4, NavigationTripOrder.Append);

        var req = Assert.Single(handler.Requests);
        Assert.EndsWith("/command/navigation_gps_request", req.Uri.AbsolutePath);
        var body = Body(req);
        Assert.Equal(52.5, body.GetProperty("lat").GetDouble());
        Assert.Equal(13.4, body.GetProperty("lon").GetDouble());
        Assert.Equal(2, body.GetProperty("order").GetInt32());
        Assert.True(result.Result);
        Assert.Equal("", result.Reason);
    }

    [Fact]
    public async Task NavigateToSupercharger_posts_id_order()
    {
        var (client, handler) = Create("""{"response":{"result":false,"reason":"no_nav"}}""");

        var result = await client.NavigateToSuperchargerAsync(Vin, 123456789012, NavigationTripOrder.Prepend);

        var req = Assert.Single(handler.Requests);
        Assert.EndsWith("/command/navigation_sc_request", req.Uri.AbsolutePath);
        Assert.Equal(123456789012, Body(req).GetProperty("id").GetInt64());
        Assert.Equal(1, Body(req).GetProperty("order").GetInt32());
        Assert.False(result.Result);
        Assert.Equal("no_nav", result.Reason);
    }

    [Fact]
    public async Task SetNavigationWaypoints_joins_ref_ids()
    {
        var (client, handler) = Create("""{"response":{"result":true,"reason":""}}""");

        await client.SetNavigationWaypointsAsync(Vin, ["ChIJa", "ChIJb"]);

        var req = Assert.Single(handler.Requests);
        Assert.EndsWith("/command/navigation_waypoints_request", req.Uri.AbsolutePath);
        Assert.Equal("refId:ChIJa,refId:ChIJb", Body(req).GetProperty("waypoints").GetString());
    }

    [Fact]
    public async Task SetNavigationWaypoints_rejects_empty_list()
    {
        var (client, handler) = Create("{}");
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetNavigationWaypointsAsync(Vin, []));
        Assert.Empty(handler.Requests);
    }

    public static IEnumerable<object[]> Mutations() =>
    [
        [(Func<CoilProClient, Task>)(c => c.ShareNavigationDestinationAsync(Vin, "x"))],
        [(Func<CoilProClient, Task>)(c => c.NavigateToCoordinatesAsync(Vin, 1, 2))],
        [(Func<CoilProClient, Task>)(c => c.NavigateToSuperchargerAsync(Vin, 1))],
        [(Func<CoilProClient, Task>)(c => c.SetNavigationWaypointsAsync(Vin, ["a"]))],
        [(Func<CoilProClient, Task>)(c => c.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = EnterprisePayerRole.Charging, AccountId = "a" }))],
    ];

    [Theory]
    [MemberData(nameof(Mutations))]
    public async Task Mutations_are_not_retried_on_503(Func<CoilProClient, Task> call)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("""{"error":"timeout"}""", HttpStatusCode.ServiceUnavailable));
        var client = CoilProClientFactory.Create(handler, options: new CoilProClientOptions { MaxRetries = 3, BaseRetryDelay = TimeSpan.Zero });

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => call(client));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Single(handler.Requests);
    }

    // ---- Users ----

    [Fact]
    public async Task GetMe_parses_spec_example()
    {
        var (client, handler) = Create("""{"response":{"email":"test-user@tesla.com","full_name":"Testy McTesterson","profile_image_url":"https://x/y.jpg","vault_uuid":"b5c443af"}}""");

        var me = await client.GetMeAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal($"{Base}/api/1/users/me", handler.LastRequest.Uri.ToString());
        Assert.Equal("test-user@tesla.com", me.Email);
        Assert.Equal("Testy McTesterson", me.FullName);
        Assert.Equal("https://x/y.jpg", me.ProfileImageUrl);
        Assert.Equal("b5c443af", me.VaultUuid);
    }

    [Fact]
    public async Task GetOrders_parses_camelCase_array()
    {
        var (client, handler) = Create("""{"response":[{"vehicleMapId":1234466,"referenceNumber":"RN00000001","vin":"5YJ30000000000001","orderStatus":"BOOKED","orderSubstatus":"_Z","modelCode":"m3","countryCode":"US","locale":"en_US","mktOptions":"APBS,DV2W","isB2b":false}],"count":1}""");

        var orders = await client.GetOrdersAsync();

        Assert.Equal($"{Base}/api/1/users/orders", handler.LastRequest!.Uri.ToString());
        var o = Assert.Single(orders);
        Assert.Equal(1234466, o.VehicleMapId);
        Assert.Equal("RN00000001", o.ReferenceNumber);
        Assert.Equal("BOOKED", o.OrderStatus);
        Assert.Equal("m3", o.ModelCode);
        Assert.Equal("APBS,DV2W", o.MarketingOptions);
        Assert.False(o.IsBusiness);
    }

    [Fact]
    public async Task GetRegion_parses_spec_example()
    {
        var (client, handler) = Create("""{"response":{"region":"eu","fleet_api_base_url":"https://fleet-api.prd.eu.vn.cloud.tesla.com"}}""");

        var region = await client.GetRegionAsync();

        Assert.Equal($"{Base}/api/1/users/region", handler.LastRequest!.Uri.ToString());
        Assert.Equal("eu", region.Region);
        Assert.Equal("https://fleet-api.prd.eu.vn.cloud.tesla.com", region.FleetApiBaseUrl);
    }

    // ---- Charging ----

    [Fact]
    public async Task GetChargingInvoice_returns_pdf_bytes()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4 test");
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(pdf) });
        var client = CoilProClientFactory.Create(handler);

        var bytes = await client.GetChargingInvoiceAsync("abc-123-efg");

        Assert.Equal($"{Base}/api/1/dx/charging/invoice/abc-123-efg", handler.LastRequest!.Uri.ToString());
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal(pdf, bytes);
    }

    [Fact]
    public async Task GetChargingSessions_sends_query_and_parses_enveloped_payload()
    {
        var (client, handler) = Create("""{"response":{"data":[{"charging_periods":[{"dimensions":[{"type":"ENERGY","volume":12.5}],"start_date_time":"2026-01-01T10:00:00Z"}],"id":"s1","location":{"country":"DE","name":"Berlin"},"model":"m3","start_date_time":"a","stop_date_time":"b","tariffs":{"currency":"EUR","elements":[{"price_components":[{"price":0.4,"step_size":1,"type":"ENERGY"}],"restrictions":{}}]},"total_cost":{"excl_vat":10,"incl_vat":11.9,"vat":1.9},"total_energy":12.5,"total_time":30,"vin":"V1"}],"status_code":1000,"status_message":"ok","timestamp":{"time.Time":"x"}}}""");

        var page = await client.GetChargingSessionsAsync(new ChargingSessionsQuery
        {
            Vin = Vin,
            DateFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Limit = 10,
            Offset = 20,
        });

        var q = handler.LastRequest!.QueryValues;
        Assert.Equal("/api/1/dx/charging/sessions", handler.LastRequest.Uri.AbsolutePath);
        Assert.Equal(Vin, q["vin"]);
        Assert.Equal("10", q["limit"]);
        Assert.Equal("20", q["offset"]);
        Assert.Contains("date_from", q.Keys);
        Assert.DoesNotContain("date_to", q.Keys);
        var s = Assert.Single(page.Data!);
        Assert.Equal("s1", s.Id);
        Assert.Equal("Berlin", s.Location!.Name);
        Assert.Equal(11.9m, s.TotalCost!.InclVat);
        Assert.Equal(12.5m, s.ChargingPeriods![0].Dimensions![0].Volume);
        Assert.Equal(0.4m, s.Tariffs!.Elements![0].PriceComponents![0].Price);
        Assert.Equal(1000, page.StatusCode);
    }

    [Fact]
    public async Task GetChargingSessions_accepts_bare_payload()
    {
        var (client, handler) = Create("""{"data":[{"id":"s2","vin":"V"}],"status_code":1000}""");

        var page = await client.GetChargingSessionsAsync();

        Assert.Equal($"{Base}/api/1/dx/charging/sessions", handler.LastRequest!.Uri.ToString());
        Assert.Equal("s2", Assert.Single(page.Data!).Id);
    }

    // ---- Vehicle management ----

    [Fact]
    public async Task GetVehicleOptions_parses_codes()
    {
        var (client, handler) = Create("""{"response":{"codes":[{"code":"$MT315","displayName":"Long Range AWD","isActive":true},{"code":"$PPSW","colorCode":"PPSW","displayName":"Pearl White","isActive":true}]}}""");

        var options = await client.GetVehicleOptionsAsync(Vin);

        Assert.Equal($"{Base}/api/1/dx/vehicles/options?vin={Vin}", handler.LastRequest!.Uri.ToString());
        Assert.Equal(2, options.Count);
        Assert.Equal("$MT315", options[0].Code);
        Assert.Equal("Long Range AWD", options[0].DisplayName);
        Assert.True(options[0].IsActive);
        Assert.Equal("PPSW", options[1].ColorCode);
    }

    [Fact]
    public async Task GetWarrantyDetails_parses_spec_example()
    {
        var (client, handler) = Create("""{"response":{"activeWarranty":[{"warrantyType":"BATTERY_WARRANTY","warrantyDisplayName":"Battery Limited Warranty","expirationDate":"2029-10-21T00:00:00Z","expirationOdometer":120000,"odometerUnit":"MI","warrantyExpiredOn":null,"coverageAgeInYears":8}],"upcomingWarranty":[],"expiredWarranty":[]}}""");

        var w = await client.GetWarrantyDetailsAsync(Vin);

        Assert.Equal($"{Base}/api/1/dx/warranty/details?vin={Vin}", handler.LastRequest!.Uri.ToString());
        var a = Assert.Single(w.ActiveWarranty!);
        Assert.Equal("BATTERY_WARRANTY", a.WarrantyType);
        Assert.Equal(new DateTimeOffset(2029, 10, 21, 0, 0, 0, TimeSpan.Zero), a.ExpirationDate);
        Assert.Equal(120000, a.ExpirationOdometer);
        Assert.Equal("MI", a.OdometerUnit);
        Assert.Null(a.WarrantyExpiredOn);
        Assert.Equal(8, a.CoverageAgeInYears);
        Assert.Empty(w.UpcomingWarranty!);
    }

    [Fact]
    public async Task GetSubscriptionEligibility_parses_spec_example()
    {
        var (client, handler) = Create("""{"response":{"country":"DE","eligible":[{"addons":[{"billingPeriod":"MONTHLY","currencyCode":"EUR","optionCode":"A","price":1.5,"tax":0.3,"total":1.8}],"billingOptions":[{"billingPeriod":"YEARLY","currencyCode":"EUR","optionCode":"B","price":10,"tax":2,"total":12}],"optionCode":"CPF1","product":"Premium Connectivity","startDate":"2026-01-01"}],"vin":"V"}}""");

        var e = await client.GetSubscriptionEligibilityAsync(Vin);

        Assert.Equal($"{Base}/api/1/dx/vehicles/subscriptions/eligibility?vin={Vin}", handler.LastRequest!.Uri.ToString());
        Assert.Equal("DE", e.Country);
        var offer = Assert.Single(e.Eligible!);
        Assert.Equal("Premium Connectivity", offer.Product);
        Assert.Equal(1.8m, offer.Addons![0].Total);
        Assert.Equal("YEARLY", offer.BillingOptions![0].BillingPeriod);
    }

    [Fact]
    public async Task GetUpgradeEligibility_parses_spec_example()
    {
        var (client, handler) = Create("""{"response":{"vin":"V","country":"US","type":"VEHICLE","eligible":[{"optionCode":"$FM3U","optionGroup":"PERF_FIRMWARE","currentOptionCode":"$FM3B","pricing":[{"price":2000,"total":2000,"currencyCode":"USD","isPrimary":true}]}]}}""");

        var u = await client.GetUpgradeEligibilityAsync(Vin);

        Assert.Equal($"{Base}/api/1/dx/vehicles/upgrades/eligibility?vin={Vin}", handler.LastRequest!.Uri.ToString());
        Assert.Equal("VEHICLE", u.Type);
        var o = Assert.Single(u.Eligible!);
        Assert.Equal("$FM3U", o.OptionCode);
        Assert.Equal(2000m, o.Pricing![0].Price);
        Assert.True(o.Pricing[0].IsPrimary);
    }

    private const string RolesJson = """{"vin":"V","owner":{"entity_type":"BUSINESS","account_id":"9162","company_name":"Acme"},"drivers":[{"entity_type":"PERSON","federation_id":"4362","email":"u1@example.com"}],"payers":{"subscription":{"entity_type":"BUSINESS","account_id":"9162","company_name":"Acme"},"charging":{"entity_type":"PERSON","federation_id":"4362","email":"u1@example.com"}}}""";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEnterpriseRoles_parses_bare_and_enveloped(bool enveloped)
    {
        var (client, handler) = Create(enveloped ? $$"""{"response":{{RolesJson}}}""" : RolesJson);

        var roles = await client.GetEnterpriseRolesAsync(Vin);

        Assert.Equal($"{Base}/api/1/dx/enterprise/v1/{Vin}/roles", handler.LastRequest!.Uri.ToString());
        Assert.Equal("Acme", roles.Owner!.CompanyName);
        Assert.Equal("u1@example.com", Assert.Single(roles.Drivers!).Email);
        Assert.Equal("PERSON", roles.Payers!.Charging!.EntityType);
        Assert.Equal("BUSINESS", roles.Payers.Subscription!.EntityType);
    }

    [Theory]
    [InlineData(EnterprisePayerRole.Charging, "CHARGING")]
    [InlineData(EnterprisePayerRole.Subscription, "SUBSCRIPTION")]
    public async Task SetEnterprisePayer_posts_role_and_account(EnterprisePayerRole role, string expected)
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CoilProClientFactory.Create(handler);

        await client.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = role, AccountId = "acc-1" });

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"{Base}/api/1/dx/enterprise/v1/{Vin}/payer", req.Uri.ToString());
        var body = Body(req);
        Assert.Equal(expected, body.GetProperty("role").GetString());
        Assert.Equal("acc-1", body.GetProperty("account_id").GetString());
        Assert.False(body.TryGetProperty("federation_id", out _));
    }

    [Fact]
    public async Task SetEnterprisePayer_sends_federation_id()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CoilProClientFactory.Create(handler);

        await client.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = EnterprisePayerRole.Charging, FederationId = "fed-1" });

        Assert.Equal("fed-1", Body(handler.LastRequest!).GetProperty("federation_id").GetString());
    }

    [Fact]
    public async Task SetEnterprisePayer_requires_exactly_one_id()
    {
        var (client, handler) = Create("{}");
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = EnterprisePayerRole.Charging }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = EnterprisePayerRole.Charging, AccountId = "a", FederationId = "f" }));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SetEnterprisePayer_maps_412_to_exception()
    {
        var (client, _) = Create("""{"error":"BUSINESS_MISSING_PAYMENT_METHOD"}""", HttpStatusCode.PreconditionFailed);
        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.SetEnterprisePayerAsync(Vin, new EnterprisePayerRequest { Role = EnterprisePayerRole.Charging, AccountId = "a" }));
        Assert.Equal("BUSINESS_MISSING_PAYMENT_METHOD", ex.ErrorCode);
    }

    // ---- Status ----

    [Fact]
    public async Task GetApiStatus_true_on_plain_text_ok()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok", Encoding.UTF8, "text/plain") });
        var client = CoilProClientFactory.Create(handler);

        Assert.True(await client.GetApiStatusAsync());
        Assert.Equal($"{Base}/status", handler.LastRequest!.Uri.ToString());
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }

    [Fact]
    public async Task GetApiStatus_false_on_other_body_or_error()
    {
        var other = CoilProClientFactory.Create(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("degraded") }));
        Assert.False(await other.GetApiStatusAsync());

        var failing = CoilProClientFactory.Create(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>") }),
            options: new CoilProClientOptions { MaxRetries = 0 });
        Assert.False(await failing.GetApiStatusAsync());
    }
}
