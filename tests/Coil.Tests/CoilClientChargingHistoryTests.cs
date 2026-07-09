using Coil.Tests.TestSupport;

namespace Coil.Tests;

// The charging-history payload shape is explicitly provisional (see ChargingHistoryPage's XML docs).
// These tests assume the same `{ "response": ... }` envelope used by every other Fleet API endpoint
// in this SDK (GetVehiclesAsync, GetVehicleDataAsync, WakeUpAsync); if the real implementation instead
// returns the bare `{ "data": [...] }` shape without an envelope, only this file's fixture/assumption
// needs updating, not the ChargingHistoryPage/ChargingRecord models themselves.
public class CoilClientChargingHistoryTests
{
    private const string ChargingHistoryJson = """
        {
          "response": {
            "data": [
              { "vin": "5YJ3E1EA1JF000001", "chargeStartDateTime": "2026-01-01T10:00:00Z", "chargeStopDateTime": "2026-01-01T11:00:00Z", "siteLocationName": "Home" },
              { "vin": "5YJ3E1EA1JF000001", "chargeStartDateTime": "2026-01-02T08:00:00Z", "chargeStopDateTime": "2026-01-02T08:45:00Z", "siteLocationName": "Supercharger Downtown" }
            ],
            "totalResults": 2,
            "hasMoreData": false
          }
        }
        """;

    [Fact]
    public async Task GetChargingHistoryAsync_RequestsExpectedPath()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ChargingHistoryJson);
        var client = CoilClientFactory.Create(handler, new FakeTeslaTokenProvider("tok"));

        await client.GetChargingHistoryAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/1/dx/charging/history", request.Uri.AbsolutePath);
        Assert.Equal("Bearer tok", request.AuthorizationHeader);
    }

    [Fact]
    public async Task GetChargingHistoryAsync_ParsesRecordsAndPagingMetadata()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ChargingHistoryJson);
        var client = CoilClientFactory.Create(handler);

        var page = await client.GetChargingHistoryAsync();

        Assert.Equal(2, page.Data.Count);
        Assert.Equal("5YJ3E1EA1JF000001", page.Data[0].Vin);
        Assert.Equal("Home", page.Data[0].SiteLocationName);
        Assert.Equal("Supercharger Downtown", page.Data[1].SiteLocationName);
        Assert.Equal(2, page.TotalResults);
        Assert.Equal(false, page.HasMoreData);
    }

    [Fact]
    public async Task GetChargingHistoryAsync_WithEmptyData_ReturnsEmptyList()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{ "response": { "data": [] } }""");
        var client = CoilClientFactory.Create(handler);

        var page = await client.GetChargingHistoryAsync();

        Assert.Empty(page.Data);
    }
}
