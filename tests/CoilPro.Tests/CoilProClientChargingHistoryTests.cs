using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests;

// Fixture mirrors the live EU response of 2026-11-24 (values anonymized): the charging-history endpoint
// returns { "data": [...], "totalResults": n } at the top level — no { "response": ... } envelope.
public class CoilProClientChargingHistoryTests
{
    private const string ChargingHistoryJson = """
        {
          "data": [
            {
              "sessionId": 760927307,
              "vin": "5YJ3E1EA1JF000001",
              "siteLocationName": "Irxleben, Germany",
              "chargeStartDateTime": "2026-09-11T17:07:25+02:00",
              "chargeStopDateTime": "2026-09-11T17:24:40+02:00",
              "unlatchDateTime": "2026-09-11T17:24:42+02:00",
              "countryCode": "DE",
              "fees": [
                {
                  "sessionFeeId": 1510328681, "feeType": "CHARGING", "currencyCode": "EUR", "pricingType": "PAYMENT",
                  "rateBase": 0.52, "rateTier1": 0, "rateTier2": 0, "rateTier3": null, "rateTier4": null,
                  "usageBase": 31.4, "usageTier1": 0, "usageTier2": 0, "usageTier3": null, "usageTier4": null,
                  "totalBase": 16.33, "totalTier1": 0, "totalTier2": 0, "totalTier3": 0, "totalTier4": 0,
                  "totalDue": 16.33, "netDue": 16.33, "uom": "kwh", "isPaid": true, "status": "PAID"
                }
              ],
              "billingType": "IMMEDIATE",
              "invoices": [ { "fileName": "invoice.pdf", "contentId": "abc-123", "invoiceType": "IMMEDIATE" } ],
              "vehicleMakeType": "TSLA"
            },
            { "sessionId": 760000001, "vin": "5YJ3E1EA1JF000001", "siteLocationName": "Home", "fees": [], "invoices": [] }
          ],
          "totalResults": 2
        }
        """;

    [Fact]
    public async Task GetChargingHistoryAsync_RequestsExpectedPath()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ChargingHistoryJson);
        var client = CoilProClientFactory.Create(handler, new FakeTeslaTokenProvider("tok"));

        await client.GetChargingHistoryAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/1/dx/charging/history", request.Uri.AbsolutePath);
        Assert.Equal("Bearer tok", request.AuthorizationHeader);
    }

    [Fact]
    public async Task GetChargingHistoryAsync_ParsesBareResponse_RecordsFeesAndInvoices()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ChargingHistoryJson);
        var client = CoilProClientFactory.Create(handler);

        var page = await client.GetChargingHistoryAsync();

        Assert.Equal(2, page.TotalResults);
        Assert.Null(page.HasMoreData);
        Assert.Equal(2, page.Data.Count);

        var record = page.Data[0];
        Assert.Equal(760927307L, record.SessionId);
        Assert.Equal("Irxleben, Germany", record.SiteLocationName);
        Assert.Equal("DE", record.CountryCode);
        Assert.Equal("IMMEDIATE", record.BillingType);
        Assert.Equal("2026-09-11T17:24:42+02:00", record.UnlatchDateTime);

        var fee = Assert.Single(record.Fees);
        Assert.Equal("CHARGING", fee.FeeType);
        Assert.Equal("EUR", fee.CurrencyCode);
        Assert.Equal("kwh", fee.Uom);
        Assert.Equal(31.4m, fee.UsageBase);
        Assert.Equal(16.33m, fee.TotalDue);
        Assert.True(fee.IsPaid);
        Assert.NotNull(fee.AdditionalData);
        Assert.True(fee.AdditionalData!.ContainsKey("rateTier1"));

        var invoice = Assert.Single(record.Invoices);
        Assert.Equal("abc-123", invoice.ContentId);

        Assert.Empty(page.Data[1].Fees);
    }

    [Fact]
    public async Task GetChargingHistoryAsync_WithEmptyData_ReturnsEmptyList()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{ "data": [], "totalResults": 0 }""");
        var client = CoilProClientFactory.Create(handler);

        var page = await client.GetChargingHistoryAsync();

        Assert.Empty(page.Data);
        Assert.Equal(0, page.TotalResults);
    }
}
