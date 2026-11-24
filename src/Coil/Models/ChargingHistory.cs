using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil;

/// <summary>
/// A page of Supercharger charging history from <c>GET api/1/dx/charging/history</c>.
/// </summary>
/// <remarks>
/// Shape confirmed against the live EU API on 2026-11-24 (see <c>docs/api-deviations.md</c>): the endpoint
/// answers <c>{ "data": [...], "totalResults": n }</c> at the top level, without the usual
/// <c>{ "response": ... }</c> envelope. Unmodeled fields are preserved in <see cref="AdditionalData"/>.
/// </remarks>
public sealed record ChargingHistoryPage
{
    /// <summary>The charging records in this page.</summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<ChargingRecord> Data { get; init; } = [];

    /// <summary>Total number of records across all pages, if reported.</summary>
    [JsonPropertyName("totalResults")]
    public int? TotalResults { get; init; }

    /// <summary>
    /// Whether more pages are available, if reported. Not present in the live EU response of 2026-11-24;
    /// compare <see cref="TotalResults"/> with the records received instead.
    /// </summary>
    [JsonPropertyName("hasMoreData")]
    public bool? HasMoreData { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A single charging session.</summary>
public sealed record ChargingRecord
{
    /// <summary>Tesla's id of the charging session.</summary>
    [JsonPropertyName("sessionId")] public long? SessionId { get; init; }

    [JsonPropertyName("vin")] public string? Vin { get; init; }

    /// <summary>Human-readable site name, e.g. <c>Irxleben, Germany</c>.</summary>
    [JsonPropertyName("siteLocationName")] public string? SiteLocationName { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code of the site.</summary>
    [JsonPropertyName("countryCode")] public string? CountryCode { get; init; }

    /// <summary>Start of charging, ISO 8601 with offset as sent by Tesla.</summary>
    [JsonPropertyName("chargeStartDateTime")] public string? ChargeStartDateTime { get; init; }

    /// <summary>End of charging, ISO 8601 with offset as sent by Tesla.</summary>
    [JsonPropertyName("chargeStopDateTime")] public string? ChargeStopDateTime { get; init; }

    /// <summary>When the connector was unlatched (relevant for idle/congestion fees).</summary>
    [JsonPropertyName("unlatchDateTime")] public string? UnlatchDateTime { get; init; }

    /// <summary>Billing mode, e.g. <c>IMMEDIATE</c>.</summary>
    [JsonPropertyName("billingType")] public string? BillingType { get; init; }

    /// <summary>The fees billed for this session (charging, congestion, ...).</summary>
    [JsonPropertyName("fees")] public IReadOnlyList<ChargingFee> Fees { get; init; } = [];

    /// <summary>Invoice documents for this session.</summary>
    [JsonPropertyName("invoices")] public IReadOnlyList<ChargingInvoice> Invoices { get; init; } = [];

    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>
/// One fee line of a charging session. Tier fields (<c>rateTier1</c>…<c>totalTier4</c>) are kept in
/// <see cref="AdditionalData"/>; the base and total amounts cover the common case.
/// </summary>
public sealed record ChargingFee
{
    [JsonPropertyName("sessionFeeId")] public long? SessionFeeId { get; init; }

    /// <summary>Kind of fee, e.g. <c>CHARGING</c> or <c>CONGESTION</c>.</summary>
    [JsonPropertyName("feeType")] public string? FeeType { get; init; }

    /// <summary>ISO 4217 currency code, e.g. <c>EUR</c>.</summary>
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; init; }

    /// <summary>E.g. <c>PAYMENT</c>.</summary>
    [JsonPropertyName("pricingType")] public string? PricingType { get; init; }

    /// <summary>Unit of <see cref="UsageBase"/>, e.g. <c>kwh</c> or <c>min</c>.</summary>
    [JsonPropertyName("uom")] public string? Uom { get; init; }

    [JsonPropertyName("rateBase")] public decimal? RateBase { get; init; }

    [JsonPropertyName("usageBase")] public decimal? UsageBase { get; init; }

    [JsonPropertyName("totalBase")] public decimal? TotalBase { get; init; }

    [JsonPropertyName("totalDue")] public decimal? TotalDue { get; init; }

    [JsonPropertyName("netDue")] public decimal? NetDue { get; init; }

    [JsonPropertyName("isPaid")] public bool? IsPaid { get; init; }

    [JsonPropertyName("status")] public string? Status { get; init; }

    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Reference to an invoice document of a charging session.</summary>
public sealed record ChargingInvoice
{
    [JsonPropertyName("fileName")] public string? FileName { get; init; }

    /// <summary>Id to download the invoice document with.</summary>
    [JsonPropertyName("contentId")] public string? ContentId { get; init; }

    [JsonPropertyName("invoiceType")] public string? InvoiceType { get; init; }

    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
