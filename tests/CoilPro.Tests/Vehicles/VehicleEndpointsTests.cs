using System.Net;
using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests.Vehicles;

public class VehicleEndpointsTests
{
    private const string Vin = "5YJ3E1EA1JF000001";

    private static (CoilProClient Client, StubHttpMessageHandler Handler) Setup(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = StubHttpMessageHandler.ReturningJson(json, status);
        return (CoilProClientFactory.Create(handler), handler);
    }

    private static void AssertRequest(StubHttpMessageHandler handler, HttpMethod method, string path)
    {
        var request = Assert.Single(handler.Requests);
        Assert.Equal(method, request.Method);
        Assert.Equal(path, request.Uri.AbsolutePath);
    }

    private const string VehicleJson = """
        {"id":100021,"vehicle_id":99999,"vin":"TEST00000000VIN01","color":null,"access_type":"OWNER","display_name":"Owned","option_codes":"TEST0,COUS","granular_access":{"hide_private":false},"tokens":["4f993c5b9e2b937b"],"state":"online","in_service":false,"id_s":"100021","calendar_enabled":true,"api_version":null,"backseat_token":null,"backseat_token_updated_at":null}
        """;

    [Fact]
    public async Task GetVehiclesPage_SendsPagingQuery_AndParsesPagination()
    {
        var (client, handler) = Setup($$"""
            {"response":[{{VehicleJson}}],"pagination":{"previous":null,"next":2,"current":1,"per_page":1,"count":null,"pages":null},"count":1}
            """);

        var page = await client.GetVehiclesPageAsync(page: 1, perPage: 1);

        AssertRequest(handler, HttpMethod.Get, "/api/1/vehicles");
        Assert.Equal("1", handler.LastRequest!.QueryValues["page"]);
        Assert.Equal("1", handler.LastRequest.QueryValues["per_page"]);
        var vehicle = Assert.Single(page.Items);
        Assert.Equal("OWNER", vehicle.AccessType);
        Assert.False(vehicle.GranularAccess!.HidePrivate);
        Assert.Equal("TEST0,COUS", vehicle.OptionCodes);
        Assert.Equal(2, page.Pagination!.Next);
        Assert.Equal(1, page.Pagination.Current);
        Assert.Equal(1, page.Count);
    }

    [Fact]
    public async Task GetVehicle_UsesEscapedVinPath()
    {
        var (client, handler) = Setup($$"""{"response":{{VehicleJson}}}""");

        var vehicle = await client.GetVehicleAsync("VIN/1 2");

        Assert.Equal("/api/1/vehicles/VIN%2F1%202", handler.LastRequest!.Uri.AbsolutePath.Replace("%252F", "%2F"));
        Assert.Equal("online", vehicle.State);
        Assert.Equal("100021", vehicle.IdString);
    }

    [Fact]
    public async Task GetVehicleDataCached_ParsesBlocks()
    {
        var (client, handler) = Setup("""
            {"response":{"id":1,"vin":"V","state":"asleep","charge_state":{"battery_level":48,"battery_range":127.62,"timestamp":1792631068690},
            "vehicle_state":{"odometer":1234.5,"tpms_pressure_fl":2.825,"software_update":{"status":"","version":" ","download_perc":0,"install_perc":1,"expected_duration_sec":2700},"df":0,"locked":true}}}
            """);

        var data = await client.GetVehicleDataCachedAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/vehicle_data_cached");
        Assert.Equal(48, data.ChargeState!.BatteryLevel);
        Assert.Equal(1792631068690, data.ChargeState.Timestamp);
        Assert.Equal(2.825, data.VehicleState!.TpmsPressureFl);
        Assert.Equal(2700, data.VehicleState.SoftwareUpdate!.ExpectedDurationSec);
        Assert.Null(data.DriveState);
    }

    [Fact]
    public void VehicleDataEndpoint_NewValues_SerializeWithSemicolons()
    {
        var value = (VehicleDataEndpoint.ChargeScheduleData | VehicleDataEndpoint.PreconditioningScheduleData | VehicleDataEndpoint.VehicleDataCombo).ToQueryValue();
        Assert.Equal("charge_schedule_data;preconditioning_schedule_data;vehicle_data_combo", value);
    }

    [Fact]
    public async Task GetNearbyChargingSites_SendsQuery_AndParsesSites()
    {
        var (client, handler) = Setup("""
            {"response":{"congestion_sync_time_utc_secs":1693588513,"destination_charging":[{"location":{"lat":37.4,"long":-122.1},"name":"Hilton","type":"destination","distance_miles":1.35,"amenities":"wifi"}],
            "superchargers":[{"location":{"lat":37.39,"long":-122.11},"name":"Los Altos, CA","type":"supercharger","distance_miles":2.2,"available_stalls":12,"total_stalls":16,"site_closed":false,"amenities":"restrooms","billing_info":""}],"timestamp":1693588576552}}
            """);

        var sites = await client.GetNearbyChargingSitesAsync(Vin, new NearbyChargingSitesQuery { Count = 5, RadiusMiles = 20, Detail = true });

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/nearby_charging_sites");
        var q = handler.LastRequest!.QueryValues;
        Assert.Equal("5", q["count"]);
        Assert.Equal("20", q["radius"]);
        Assert.Equal("true", q["detail"]);
        Assert.Equal(1693588576552, sites.Timestamp);
        Assert.Equal(-122.1, sites.DestinationCharging![0].Location!.Longitude);
        Assert.Equal(12, sites.Superchargers![0].AvailableStalls);
        Assert.False(sites.Superchargers[0].SiteClosed);
    }

    [Fact]
    public async Task GetNearbyChargingSites_WithoutQuery_SendsNoQueryString()
    {
        var (client, handler) = Setup("""{"response":{}}""");
        await client.GetNearbyChargingSitesAsync(Vin);
        Assert.Equal(string.Empty, handler.LastRequest!.Uri.Query);
    }

    [Fact]
    public async Task GetRecentAlerts_UnwrapsAlertList()
    {
        var (client, handler) = Setup("""
            {"response":{"recent_alerts":[{"name":"UI_a213_ISADisabled","time":"Tue, 24 Nov 2026 09:27:27 +0000","audiences":["customer","service"],"user_text":"Intelligent Speed Assist disabled"}]}}
            """);

        var alerts = await client.GetRecentAlertsAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/recent_alerts");
        var alert = Assert.Single(alerts);
        Assert.Equal("UI_a213_ISADisabled", alert.Name);
        // Shape as observed live on 2026-11-24 (RFC 1123 time, "audiences").
        Assert.Equal(new DateTimeOffset(2026, 11, 24, 9, 27, 27, TimeSpan.Zero), alert.TimeUtc);
        Assert.Equal(["customer", "service"], alert.Audiences);
    }

    [Theory]
    [InlineData("""{"response":{"release_notes":[{"title":"Minor Fixes","customer_version":"2022.42.0"}]}}""")]
    [InlineData("""{"response":{"response":{"release_notes":[{"title":"Minor Fixes","customer_version":"2022.42.0"}]}}}""")]
    public async Task GetReleaseNotes_ParsesFlatAndDoubleNestedShape(string json)
    {
        var (client, handler) = Setup(json);

        var notes = await client.GetReleaseNotesAsync(Vin, staged: true, language: "en_US");

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/release_notes");
        Assert.Equal("true", handler.LastRequest!.QueryValues["staged"]);
        Assert.Equal("en_US", handler.LastRequest.QueryValues["language"]);
        Assert.Equal("2022.42.0", Assert.Single(notes).CustomerVersion);
    }

    [Fact]
    public async Task GetReleaseNotes_Defaults_OmitQuery()
    {
        var (client, handler) = Setup("""{"response":{"release_notes":[]}}""");
        Assert.Empty(await client.GetReleaseNotesAsync(Vin));
        Assert.Equal(string.Empty, handler.LastRequest!.Uri.Query);
    }

    [Fact]
    public async Task GetServiceData_Parses()
    {
        var (client, handler) = Setup("""{"response":{"service_status":"car_in_repair","service_etc":"2023-05-02T17:10:53-10:00","service_visit_number":"SV12345678","status_id":8}}""");

        var data = await client.GetServiceDataAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/service_data");
        Assert.Equal("car_in_repair", data.ServiceStatus);
        Assert.Equal(8, data.StatusId);
        Assert.Equal(TimeSpan.FromHours(-10), data.ServiceEtc!.Value.Offset);
    }

    [Fact]
    public async Task GetServiceHistory_ParsesPaging()
    {
        var (client, handler) = Setup("""
            {"response":[{"service_visit_id":12345679,"service_visit_number":"SV12345679","status":"delivered","visit_type":"service_center","checked_in_at":"2023-05-15T16:02:11Z","completed_at":"2023-05-16T22:47:30Z","location":{"name":"Tesla Service Center"}}],
            "pagination":{"previous":null,"next":2,"current":1,"per_page":1,"count":null,"pages":null},"count":1}
            """);

        var page = await client.GetServiceHistoryAsync(Vin, page: 1, perPage: 20);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/service_history");
        Assert.Equal("20", handler.LastRequest!.QueryValues["per_page"]);
        var visit = Assert.Single(page.Items);
        Assert.Equal(12345679, visit.ServiceVisitId);
        Assert.Equal("service_center", visit.VisitType);
        Assert.Equal("Tesla Service Center", visit.Location!.Name);
        Assert.Equal(2, page.Pagination!.Next);
        Assert.Null(page.Pagination.Pages);
    }

    [Fact]
    public async Task GetServiceVisit_ParsesWorkItemsAndCost()
    {
        var (client, handler) = Setup("""
            {"response":{"service_visit_id":12345679,"status":"delivered","visit_type":"mobile","work_items":[{"correction_code":"C1","description":"Wiper","pay_type":"warranty"}],
            "cost":{"currency":"EUR","subtotal":10.5,"tax":2,"total":12.5,"paid":12.5,"due":0}}}
            """);

        var visit = await client.GetServiceVisitAsync(Vin, 12345679);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/service_history/12345679");
        Assert.Equal("warranty", visit.WorkItems![0].PayType);
        Assert.Equal(12.5, visit.Cost!.Total);
        Assert.Equal("EUR", visit.Cost.Currency);
    }

    [Fact]
    public async Task GetServiceVisit_NullCost_IsNull()
    {
        var (client, _) = Setup("""{"response":{"service_visit_id":1,"cost":null,"work_items":[]}}""");
        Assert.Null((await client.GetServiceVisitAsync(Vin, 1)).Cost);
    }

    [Fact]
    public async Task GetDrivers_Parses()
    {
        var (client, handler) = Setup("""
            {"response":[{"my_tesla_unique_id":8888888,"user_id":800001,"user_id_s":"800001","vault_uuid":"b5c4","driver_first_name":"Testy","driver_last_name":"McTesterson","granular_access":{"hide_private":false},"active_pubkeys":[],"public_key":""}],"count":1}
            """);

        var drivers = await client.GetDriversAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/drivers");
        var driver = Assert.Single(drivers);
        Assert.Equal(800001, driver.UserId);
        Assert.Equal("Testy", driver.FirstName);
    }

    [Fact]
    public async Task RemoveDriver_SendsDeleteWithShareUserId_AndAcceptsBareOk()
    {
        var (client, handler) = Setup("""{"response":"ok"}""");

        await client.RemoveDriverAsync(Vin, 800001);

        AssertRequest(handler, HttpMethod.Delete, $"/api/1/vehicles/{Vin}/drivers");
        Assert.Equal("800001", handler.LastRequest!.QueryValues["share_user_id"]);
    }

    [Fact]
    public async Task RemoveDriver_WithoutShareUserId_OmitsQuery()
    {
        var (client, handler) = Setup("""{"response":"ok"}""");
        await client.RemoveDriverAsync(Vin);
        Assert.Equal(string.Empty, handler.LastRequest!.Uri.Query);
    }

    [Fact]
    public async Task GetMobileEnabled_Parses()
    {
        var (client, handler) = Setup("""{"response":{"reason":"","result":true}}""");

        var status = await client.GetMobileEnabledAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/mobile_enabled");
        Assert.True(status.Result);
    }

    [Fact]
    public async Task GetMobileEnabled_ParsesBareBoolean_AsObservedLive()
    {
        var (client, _) = Setup("""{"response":true}""");

        var status = await client.GetMobileEnabledAsync(Vin);

        Assert.True(status.Result);
    }

    private const string InviteJson = """
        {"id":429509621657,"owner_id":429511308124,"share_user_id":null,"product_id":"TEST00000000VIN01","state":"pending","code":"aqwl","expires_at":"2023-06-29T00:42:00.000Z","revoked_at":null,"borrowing_device_id":null,"key_id":null,"product_type":"vehicle","share_type":"customer","share_user_sso_id":null,"active_pubkeys":[null],"id_s":"429509621657","owner_id_s":"429511308124","share_user_id_s":"","borrowing_key_hash":null,"vin":"TEST00000000VIN01","share_link":"https://www.tesla.com/_rs/1/aqwl"}
        """;

    [Fact]
    public async Task GetShareInvites_ParsesPage()
    {
        var (client, handler) = Setup($$"""
            {"response":[{{InviteJson}}],"pagination":{"previous":null,"next":null,"current":1,"per_page":25,"count":1,"pages":1},"count":1}
            """);

        var page = await client.GetShareInvitesAsync(Vin, perPage: 25);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/invitations");
        var invite = Assert.Single(page.Items);
        Assert.Equal("aqwl", invite.Code);
        Assert.Equal("pending", invite.State);
        Assert.Equal(new DateTimeOffset(2023, 6, 29, 0, 42, 0, TimeSpan.Zero), invite.ExpiresAt);
        Assert.Equal(1, page.Pagination!.Pages);
        Assert.Equal(1, page.Count);
    }

    [Fact]
    public async Task CreateShareInvite_PostsAndParses()
    {
        var (client, handler) = Setup($$"""{"response":{{InviteJson}}}""", HttpStatusCode.Created);

        var invite = await client.CreateShareInviteAsync(Vin);

        AssertRequest(handler, HttpMethod.Post, $"/api/1/vehicles/{Vin}/invitations");
        Assert.Equal("https://www.tesla.com/_rs/1/aqwl", invite.ShareLink);
    }

    [Fact]
    public async Task RedeemShareInvite_PostsCodeBody()
    {
        var (client, handler) = Setup("""{"response":{"vehicle_id_s":"88850","vin":"5YJY000000NEXUS01"}}""");

        var result = await client.RedeemShareInviteAsync("abc");

        AssertRequest(handler, HttpMethod.Post, "/api/1/invitations/redeem");
        Assert.Equal("""{"code":"abc"}""", handler.LastRequest!.Body);
        Assert.Equal("5YJY000000NEXUS01", result.Vin);
    }

    [Fact]
    public async Task RevokeShareInvite_PostsAndAcceptsBareTrue()
    {
        var (client, handler) = Setup("""{"response":true}""");

        await client.RevokeShareInviteAsync(Vin, "42");

        AssertRequest(handler, HttpMethod.Post, $"/api/1/vehicles/{Vin}/invitations/42/revoke");
    }

    [Fact]
    public async Task GetFleetStatus_PostsVins_AndParsesVehicleInfoMap()
    {
        var (client, handler) = Setup("""
            {"response":{"key_paired_vins":["5YJ3E1EA1JF000001"],"unpaired_vins":[],"vehicle_info":{"5YJ3E1EA1JF000001":{"firmware_version":"2026.38.6","vehicle_command_protocol_required":true,"discounted_device_data":false,"fleet_telemetry_version":"1.0","total_number_of_keys":2}}}}
            """);

        var status = await client.GetFleetStatusAsync([Vin]);

        AssertRequest(handler, HttpMethod.Post, "/api/1/vehicles/fleet_status");
        Assert.Equal($$"""{"vins":["{{Vin}}"]}""", handler.LastRequest!.Body);
        Assert.Equal([Vin], status.KeyPairedVins);
        var info = status.VehicleInfo![Vin];
        Assert.True(info.VehicleCommandProtocolRequired);
        Assert.Equal(2, info.TotalNumberOfKeys);
        Assert.Equal("2026.38.6", info.FirmwareVersion);
    }

    [Fact]
    public async Task GetFleetTelemetryConfig_ParsesSharedConfigShape()
    {
        var (client, handler) = Setup("""
            {"response":{"synced":true,"config":{"hostname":"test-telemetry.com","ca":"-----BEGIN CERTIFICATE-----\ncert\n-----END CERTIFICATE-----\n","port":4443,
            "fields":{"Location":{"interval_seconds":1800,"include_fields":["GpsHeading"]},"ChargerVoltage":{"interval_seconds":1,"minimum_delta":5}},"alert_types":["service"]},"limit_reached":false,"key_paired":false}}
            """);

        var status = await client.GetFleetTelemetryConfigAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/fleet_telemetry_config");
        Assert.True(status.Synced);
        Assert.False(status.LimitReached);
        Assert.Equal(4443, status.Config!.Port);
        Assert.Equal(5, status.Config.Fields["ChargerVoltage"].MinimumDelta);
        Assert.Equal(["GpsHeading"], status.Config.Fields["Location"].IncludeFields);
    }

    [Fact]
    public async Task DeleteFleetTelemetryConfig_ReturnsUpdatedVehicles()
    {
        var (client, handler) = Setup("""{"response":{"updated_vehicles":1}}""");

        var updated = await client.DeleteFleetTelemetryConfigAsync(Vin);

        AssertRequest(handler, HttpMethod.Delete, $"/api/1/vehicles/{Vin}/fleet_telemetry_config");
        Assert.Equal(1, updated);
    }

    [Fact]
    public async Task GetFleetTelemetryErrors_UnwrapsList()
    {
        var (client, handler) = Setup("""{"response":{"fleet_telemetry_errors":[{"name":"client-id","error":"msg","vin":"vin"}]}}""");

        var errors = await client.GetFleetTelemetryErrorsAsync(Vin);

        AssertRequest(handler, HttpMethod.Get, $"/api/1/vehicles/{Vin}/fleet_telemetry_errors");
        var error = Assert.Single(errors);
        Assert.Equal("client-id", error.Name);
        Assert.Equal("msg", error.Error);
    }

    [Fact]
    public void VehicleData_ParsesExtendedBlocks_AndKeepsUnknownFields()
    {
        const string json = """
            {"id":1,"user_id":7,"vin":"V","vehicle_config":{"car_type":"modely","charge_port_type":"CCS","seat_type":null,"sun_roof_installed":2,"utc_offset":3600},
            "vehicle_state":{"media_info":{"audio_volume":2.5,"now_playing_title":"x"},"speed_limit_mode":{"active":false,"current_limit_mph":85.0},"brand_new":1},
            "location_data":{"latitude":1.5,"longitude":2.5},"closures_state":{"anything":true}}
            """;

        var data = System.Text.Json.JsonSerializer.Deserialize<VehicleData>(json)!;

        Assert.Equal("modely", data.VehicleConfig!.CarType);
        Assert.Equal(3600, data.VehicleConfig.UtcOffset);
        Assert.Equal(2.5, data.VehicleState!.MediaInfo!.AudioVolume);
        Assert.Equal(85.0, data.VehicleState.SpeedLimitMode!.CurrentLimitMph);
        Assert.True(data.VehicleState.AdditionalData!.ContainsKey("brand_new"));
        Assert.Equal(1.5, data.LocationData!.Latitude);
        Assert.True(data.ClosuresState!.AdditionalData!.ContainsKey("anything"));
    }

    // ---- Mutations must not be retried on a transient failure ----------------------------------

    [Fact]
    public async Task Mutations_AreNotRetriedOn503()
    {
        var mutations = new (string Name, Func<CoilProClient, Task> Call)[]
        {
            ("RemoveDriver", c => c.RemoveDriverAsync(Vin, 1)),
            ("CreateShareInvite", c => c.CreateShareInviteAsync(Vin)),
            ("RedeemShareInvite", c => c.RedeemShareInviteAsync("abc")),
            ("RevokeShareInvite", c => c.RevokeShareInviteAsync(Vin, "1")),
            ("DeleteFleetTelemetryConfig", c => c.DeleteFleetTelemetryConfigAsync(Vin)),
        };

        foreach (var (name, call) in mutations)
        {
            var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("""{"error":"timeout"}""", HttpStatusCode.ServiceUnavailable));
            var client = CoilProClientFactory.Create(handler, options: new CoilProClientOptions { MaxRetries = 3, BaseRetryDelay = TimeSpan.Zero });

            await Assert.ThrowsAsync<TeslaApiException>(() => call(client));
            Assert.True(handler.Requests.Count == 1, $"{name} was retried ({handler.Requests.Count} requests).");
        }
    }

    [Fact]
    public async Task Reads_AreRetriedOn503()
    {
        var handler = new StubHttpMessageHandler(
        [
            StubHttpMessageHandler.JsonResponse("""{"error":"timeout"}""", HttpStatusCode.ServiceUnavailable),
            StubHttpMessageHandler.JsonResponse("""{"response":{"result":true}}"""),
        ]);
        var client = CoilProClientFactory.Create(handler, options: new CoilProClientOptions { MaxRetries = 2, BaseRetryDelay = TimeSpan.Zero });

        var status = await client.GetMobileEnabledAsync(Vin);

        Assert.True(status.Result);
        Assert.Equal(2, handler.Requests.Count);
    }
}
