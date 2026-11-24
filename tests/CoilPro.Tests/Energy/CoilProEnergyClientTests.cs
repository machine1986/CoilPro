using System.Net;
using System.Text.Json;
using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests.Energy;

public sealed class CoilProEnergyClientTests
{
    private const long SiteId = 1234567890L;
    private const string Base = "/api/1/energy_sites/1234567890/";

    private static CoilProEnergyClient Create(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(CoilProClientFactory.FleetApiBaseUrl) };
        return new CoilProEnergyClient(http, new FakeTeslaTokenProvider(), new CoilProClientOptions { BaseRetryDelay = TimeSpan.Zero });
    }

    [Fact]
    public async Task GetProducts_SplitsVehiclesAndEnergySites_DespiteIdTypeMismatch()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":[
              {"id":92345678901234,"vehicle_id":1,"vin":"5YJ3E1EA7KF000000","display_name":"Y","state":"online","device_type":"vehicle","user_id":5},
              {"energy_site_id":1234567890,"device_type":"energy","resource_type":"battery","site_name":"My Home","id":"STE20240101-00001","gateway_id":"GW1","energy_left":18020.5,"total_pack_energy":39343,"percentage_charged":45.8,"battery_power":-3090}
            ],"count":2}
            """);
        var products = await Create(handler).GetProductsAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/api/1/products", handler.LastRequest.Uri.AbsolutePath);
        Assert.Equal(2, products.Count);
        var v = Assert.Single(products.Vehicles);
        Assert.Equal(92345678901234, v.Id);
        Assert.Equal("5YJ3E1EA7KF000000", v.Vin);
        var s = Assert.Single(products.EnergySites);
        Assert.Equal(1234567890, s.EnergySiteId);
        Assert.Equal("STE20240101-00001", s.Id);
        Assert.Equal("My Home", s.SiteName);
        Assert.Equal(39343, s.TotalPackEnergy);
    }

    [Fact]
    public async Task GetProducts_EmptyArray()
    {
        var products = await Create(StubHttpMessageHandler.ReturningJson("""{"response":[],"count":0}""")).GetProductsAsync();
        Assert.Empty(products.Vehicles);
        Assert.Empty(products.EnergySites);
        Assert.Equal(0, products.Count);
    }

    [Fact]
    public async Task GetLiveStatus_ParsesSpecExample()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"solar_power":3102,"energy_left":18020.894736842107,"total_pack_energy":39343,"percentage_charged":45.80457701965307,"backup_capable":true,"battery_power":-3090,"load_power":2581,"grid_status":"Active","grid_power":2569,"island_status":"on_grid","storm_mode_active":false,"timestamp":"2023-01-01T00:00:00-08:00"}}
            """);
        var r = await Create(handler).GetLiveStatusAsync(SiteId);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Base + "live_status", handler.LastRequest.Uri.AbsolutePath);
        Assert.Equal("Bearer tok", handler.LastRequest.AuthorizationHeader);
        Assert.Equal(3102, r.SolarPower);
        Assert.Equal(-3090, r.BatteryPower);
        Assert.Equal("Active", r.GridStatus);
        Assert.Equal("on_grid", r.IslandStatus);
        Assert.False(r.StormModeActive);
        Assert.Equal(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8)), r.Timestamp);
    }

    [Fact]
    public async Task GetLiveStatus_UnwrapsDoubleNestedResponse()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"response":{"solar_power":7,"future_field":1}}}""");
        var r = await Create(handler).GetLiveStatusAsync(SiteId);
        Assert.Equal(7, r.SolarPower);
        Assert.True(r.AdditionalData!.ContainsKey("future_field"));
    }

    [Fact]
    public async Task GetSiteInfo_ParsesSpecExample()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"id":"0000000-00-A--TEST0000000DIN","site_name":"My Home","backup_reserve_percent":20,"default_real_mode":"autonomous","installation_date":"2023-01-01T00:00:00-08:00","user_settings":{"storm_mode_enabled":true},"components":{"solar":true,"solar_type":"pv_panel","battery":true,"grid":true,"backup":true,"load_meter":true,"storm_mode_capable":true,"off_grid_vehicle_charging_reserve_supported":true,"solar_value_enabled":true,"set_islanding_mode_enabled":true,"battery_type":"ac_powerwall","configurable":true},"version":"23.12.11 452c76cb","battery_count":3,"nameplate_power":15000,"nameplate_energy":40500,"installation_time_zone":"America/Los_Angeles","max_site_meter_power_ac":1000000000,"min_site_meter_power_ac":-11.726}}
            """);
        var r = await Create(handler).GetSiteInfoAsync(SiteId);

        Assert.Equal(Base + "site_info", handler.LastRequest!.Uri.AbsolutePath);
        Assert.Equal(20, r.BackupReservePercent);
        Assert.Equal("autonomous", r.DefaultRealMode);
        Assert.True(r.UserSettings!.StormModeEnabled);
        Assert.Equal("ac_powerwall", r.Components!.BatteryType);
        Assert.Equal(3, r.BatteryCount);
        Assert.Equal("America/Los_Angeles", r.InstallationTimeZone);
        Assert.Equal(-11.726, r.MinSiteMeterPowerAc);
    }

    [Fact]
    public async Task GetSiteStatus_ParsesSpecExample()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"resource_type":"gateway","site_name":"My Home","gateway_id":"G","percentage_charged":100,"battery_power":10,"storm_mode_enabled":true,"powerwall_onboarding_settings_set":true}}
            """);
        var r = await Create(handler).GetSiteStatusAsync(SiteId);

        Assert.Equal(Base + "site_status", handler.LastRequest!.Uri.AbsolutePath);
        Assert.Equal("gateway", r.ResourceType);
        Assert.Equal(100, r.PercentageCharged);
        Assert.True(r.StormModeEnabled);
    }

    [Fact]
    public async Task GetBackupTimeRemaining_ReturnsHours()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"time_remaining_hours":12.5}}""");
        var hours = await Create(handler).GetBackupTimeRemainingAsync(SiteId);

        Assert.Equal(Base + "backup_time_remaining", handler.LastRequest!.Uri.AbsolutePath);
        Assert.Equal(12.5, hours);
    }

    [Fact]
    public async Task GetBackupTimeRemaining_MissingValue_Throws()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{}}""");
        await Assert.ThrowsAsync<TeslaApiException>(() => Create(handler).GetBackupTimeRemainingAsync(SiteId));
    }

    [Fact]
    public async Task GetWallConnectorLiveStatus_ParsesSpecExample()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"wall_connectors":[{"din":"0000000-00-A--WC0000000001","wall_connector_state":8,"wall_connector_power":0}],"timestamp":"2023-01-01T00:00:00Z"}}
            """);
        var r = await Create(handler).GetWallConnectorLiveStatusAsync(SiteId);

        Assert.Equal(Base + "charger_live_status", handler.LastRequest!.Uri.AbsolutePath);
        var wc = Assert.Single(r.WallConnectors!);
        Assert.Equal("0000000-00-A--WC0000000001", wc.Din);
        Assert.Equal(8, wc.State);
        Assert.Equal(0, wc.Power);
        Assert.Equal(DateTimeOffset.Parse("2023-01-01T00:00:00Z"), r.Timestamp);
    }

    [Fact]
    public async Task GetTariffRate_ParsesSpecExample()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"code":"EXAMPLE-TOU","name":"Example Time of Use","utility":"Example Utility","currency":"USD","energy_charges":{"Summer":{"ON_PEAK":0.45,"OFF_PEAK":0.15}},"demand_charges":{"ALL":{"ALL":0}},"seasons":{"Summer":{"fromMonth":6,"fromDay":1,"toMonth":9,"toDay":30,"tou_periods":{"ON_PEAK":[{"fromDayOfWeek":0,"toDayOfWeek":4,"fromHour":16,"fromMinute":0,"toHour":21,"toMinute":0}]}}},"sell_tariff":{"code":"EXAMPLE-TOU","utility":"Example Utility","energy_charges":{"Summer":{"ON_PEAK":0.1,"OFF_PEAK":0.05}}}}}
            """);
        var t = await Create(handler).GetTariffRateAsync(SiteId);

        Assert.Equal(Base + "tariff_rate", handler.LastRequest!.Uri.AbsolutePath);
        Assert.NotNull(t);
        Assert.Equal("EXAMPLE-TOU", t.Code);
        Assert.Equal(0.45, t.EnergyCharges!["Summer"]["ON_PEAK"]);
        var season = t.Seasons!["Summer"];
        Assert.Equal(6, season.FromMonth);
        Assert.Equal(16, season.TouPeriods!["ON_PEAK"][0].FromHour);
        Assert.Equal(0.05, t.SellTariff!.EnergyCharges!["Summer"]["OFF_PEAK"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"response":null}""")]
    public async Task GetTariffRate_EmptyOrNull_ReturnsNull(string body)
    {
        Assert.Null(await Create(StubHttpMessageHandler.ReturningJson(body)).GetTariffRateAsync(SiteId));
    }

    private static readonly EnergyHistoryQuery Query = new()
    {
        Start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8)),
        End = new DateTimeOffset(2023, 1, 31, 23, 59, 59, TimeSpan.FromHours(-8)),
        Period = EnergyHistoryPeriod.Month,
        TimeZone = "America/Los_Angeles",
    };

    [Fact]
    public async Task GetBackupHistory_BuildsQueryAndParses()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"events":[{"timestamp":"2023-01-01T01:00:00-08:00","duration":3600},{"timestamp":"2023-01-02T08:00:00-08:00","duration":7200}],"total_events":2}}
            """);
        var r = await Create(handler).GetBackupHistoryAsync(SiteId, Query);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Base + "calendar_history", handler.LastRequest.Uri.AbsolutePath);
        var q = handler.LastRequest.QueryValues;
        Assert.Equal("backup", q["kind"]);
        Assert.Equal("2023-01-01T00:00:00-08:00", q["start_date"]);
        Assert.Equal("2023-01-31T23:59:59-08:00", q["end_date"]);
        Assert.Equal("month", q["period"]);
        Assert.Equal("America/Los_Angeles", q["time_zone"]);
        Assert.Equal(2, r.TotalEvents);
        Assert.Equal(2, r.Events!.Count);
        Assert.Equal(7200, r.Events[1].DurationSeconds);
    }

    [Fact]
    public async Task GetEnergyHistory_BuildsQueryAndParses()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"period":"day","time_series":[{"timestamp":"2023-06-01T01:00:00-07:00","solar_energy_exported":70940,"generator_energy_exported":0,"grid_energy_imported":521,"grid_services_energy_imported":17.53125,"grid_services_energy_exported":3.80859375,"grid_energy_exported_from_solar":43660,"grid_energy_exported_from_generator":0,"grid_energy_exported_from_battery":19,"battery_energy_exported":10030,"battery_energy_imported_from_grid":80,"battery_energy_imported_from_solar":16800,"battery_energy_imported_from_generator":0,"consumer_energy_imported_from_grid":441,"consumer_energy_imported_from_solar":10480,"consumer_energy_imported_from_battery":10011,"consumer_energy_imported_from_generator":0}]}}
            """);
        var r = await Create(handler).GetEnergyHistoryAsync(SiteId, Query with { Period = EnergyHistoryPeriod.Lifetime });

        Assert.Equal("energy", handler.LastRequest!.QueryValues["kind"]);
        Assert.Equal("lifetime", handler.LastRequest.QueryValues["period"]);
        Assert.Equal("day", r.Period);
        var e = Assert.Single(r.TimeSeries!);
        Assert.Equal(70940, e.SolarEnergyExported);
        Assert.Equal(17.53125, e.GridServicesEnergyImported);
        Assert.Equal(10011, e.ConsumerEnergyImportedFromBattery);
    }

    [Fact]
    public async Task GetChargeHistory_BuildsQueryWithoutPeriodAndParses()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"charge_history":[{"charge_start_time":{"seconds":1672560000},"charge_duration":{"seconds":12000},"energy_added_wh":25000}]}}
            """);
        var r = await Create(handler).GetChargeHistoryAsync(SiteId, Query.Start, Query.End, "Europe/Berlin");

        Assert.Equal(Base + "telemetry_history", handler.LastRequest!.Uri.AbsolutePath);
        var q = handler.LastRequest.QueryValues;
        Assert.Equal("charge", q["kind"]);
        Assert.Equal("Europe/Berlin", q["time_zone"]);
        Assert.False(q.ContainsKey("period"));
        var s = Assert.Single(r.ChargeHistory!);
        Assert.Equal(1672560000, s.ChargeStartTime!.Seconds);
        Assert.Equal(12000, s.ChargeDuration!.Seconds);
        Assert.Equal(25000, s.EnergyAddedWh);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1672560000), s.StartTime);
    }

    [Fact]
    public async Task History_BlankTimeZone_Throws()
    {
        var client = Create(StubHttpMessageHandler.ReturningJson("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetEnergyHistoryAsync(SiteId, Query with { TimeZone = " " }));
    }

    // ----- settings -----

    private static async Task<(CapturedRequest Request, StubHttpMessageHandler Handler)> RunSetting(Func<CoilProEnergyClient, Task> call)
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"response":{"code":201,"message":"Updated"}}""");
        await call(Create(handler));
        return (handler.LastRequest!, handler);
    }

    private static string Compact(string? json) => JsonSerializer.Serialize(JsonDocument.Parse(json!).RootElement);

    [Fact]
    public async Task SetBackupReserve_PostsPercent()
    {
        var (r, _) = await RunSetting(c => c.SetBackupReserveAsync(SiteId, 20));
        Assert.Equal(HttpMethod.Post, r.Method);
        Assert.Equal(Base + "backup", r.Uri.AbsolutePath);
        Assert.Equal("""{"backup_reserve_percent":20}""", Compact(r.Body));
    }

    [Fact]
    public async Task SetOffGridVehicleChargingReserve_PostsPercent()
    {
        var (r, _) = await RunSetting(c => c.SetOffGridVehicleChargingReserveAsync(SiteId, 35));
        Assert.Equal(Base + "off_grid_vehicle_charging_reserve", r.Uri.AbsolutePath);
        Assert.Equal("""{"off_grid_vehicle_charging_reserve_percent":35}""", Compact(r.Body));
    }

    [Theory]
    [InlineData(EnergyOperationMode.SelfConsumption, "self_consumption")]
    [InlineData(EnergyOperationMode.Autonomous, "autonomous")]
    public async Task SetOperationMode_PostsMode(EnergyOperationMode mode, string expected)
    {
        var (r, _) = await RunSetting(c => c.SetOperationModeAsync(SiteId, mode));
        Assert.Equal(Base + "operation", r.Uri.AbsolutePath);
        Assert.Equal($$"""{"default_real_mode":"{{expected}}"}""", Compact(r.Body));
    }

    [Fact]
    public async Task SetStormMode_PostsEnabled()
    {
        var (r, _) = await RunSetting(c => c.SetStormModeAsync(SiteId, true));
        Assert.Equal(Base + "storm_mode", r.Uri.AbsolutePath);
        Assert.Equal("""{"enabled":true}""", Compact(r.Body));
    }

    [Fact]
    public async Task SetGridImportExport_SendsOnlyNonNullFields()
    {
        var (r1, _) = await RunSetting(c => c.SetGridImportExportAsync(SiteId, true, null));
        Assert.Equal(Base + "grid_import_export", r1.Uri.AbsolutePath);
        Assert.Equal("""{"disallow_charge_from_grid_with_solar_installed":true}""", Compact(r1.Body));

        var (r2, _) = await RunSetting(c => c.SetGridImportExportAsync(SiteId, null, GridExportRule.PvOnly));
        Assert.Equal("""{"customer_preferred_export_rule":"pv_only"}""", Compact(r2.Body));

        var (r3, _) = await RunSetting(c => c.SetGridImportExportAsync(SiteId, false, GridExportRule.BatteryOk));
        using var doc = JsonDocument.Parse(r3.Body!);
        Assert.False(doc.RootElement.GetProperty("disallow_charge_from_grid_with_solar_installed").GetBoolean());
        Assert.Equal("battery_ok", doc.RootElement.GetProperty("customer_preferred_export_rule").GetString());

        var (r4, _) = await RunSetting(c => c.SetGridImportExportAsync(SiteId, null, GridExportRule.Never));
        Assert.Contains("\"never\"", r4.Body);
    }

    [Fact]
    public async Task SetGridImportExport_BothNull_Throws()
    {
        var client = Create(StubHttpMessageHandler.ReturningJson("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetGridImportExportAsync(SiteId, null, null));
    }

    [Fact]
    public async Task SetTimeOfUseSettings_WrapsTariffContent()
    {
        using var tariff = JsonDocument.Parse("""{"code":"X","seasons":{"Summer":{"fromMonth":6}}}""");
        var (r, _) = await RunSetting(c => c.SetTimeOfUseSettingsAsync(SiteId, tariff.RootElement));
        Assert.Equal(Base + "time_of_use_settings", r.Uri.AbsolutePath);
        Assert.Equal("""{"tou_settings":{"tariff_content_v2":{"code":"X","seasons":{"Summer":{"fromMonth":6}}}}}""", Compact(r.Body));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task PercentSettings_OutOfRange_Throw(int percent)
    {
        var handler = StubHttpMessageHandler.ReturningJson("{}");
        var client = Create(handler);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SetBackupReserveAsync(SiteId, percent));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SetOffGridVehicleChargingReserveAsync(SiteId, percent));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Settings_AreNotRetriedOn503()
    {
        var handler = StubHttpMessageHandler.ReturningSequence(
            StubHttpMessageHandler.JsonResponse("""{"error":"unavailable"}""", HttpStatusCode.ServiceUnavailable),
            StubHttpMessageHandler.JsonResponse("""{"response":{"code":201,"message":"Updated"}}"""));

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => Create(handler).SetBackupReserveAsync(SiteId, 20));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Reads_AreRetriedOn503()
    {
        var handler = StubHttpMessageHandler.ReturningSequence(
            StubHttpMessageHandler.JsonResponse("""{"error":"unavailable"}""", HttpStatusCode.ServiceUnavailable),
            StubHttpMessageHandler.JsonResponse("""{"response":{"solar_power":1}}"""));

        var r = await Create(handler).GetLiveStatusAsync(SiteId);

        Assert.Equal(1, r.SolarPower);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Settings_Accept204AndEmptyBody()
    {
        var handler = StubHttpMessageHandler.ReturningJson("", HttpStatusCode.NoContent);
        await Create(handler).SetStormModeAsync(SiteId, false);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetWallConnectorLiveStatus_ParsesLiveShape()
    {
        // As observed live on 2026-11-24 for a Wall Connector-only site.
        var handler = StubHttpMessageHandler.ReturningJson("""
            {"response":{"wall_connectors":[{"din":"1529455-42-G--PGT0000000000","wall_connector_state":2,"wall_connector_power":0,"wall_connector_fault_state":2,"ocpp_status":1,"powershare_session_state":0,"dry_contact_control_state":0}]}}
            """);

        var status = await Create(handler).GetWallConnectorLiveStatusAsync(42);

        var wc = Assert.Single(status.WallConnectors!);
        Assert.Equal(2, wc.State);
        Assert.Equal(2, wc.FaultState);
        Assert.Equal(1, wc.OcppStatus);
    }

    [Fact]
    public async Task GetTariffRate_WallConnectorOnlySite_400NotFound_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            """{"response":null,"error":"https://powergate.prd.sn.tesla.services:443/api/v4/energy_site/tariff_rate => {Message: \"Not found\", Status: 400}"}""",
            System.Net.HttpStatusCode.BadRequest);

        Assert.Null(await Create(handler).GetTariffRateAsync(42));
    }
}
