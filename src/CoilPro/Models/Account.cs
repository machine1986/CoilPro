using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>Result of an unsigned REST command: <c>{ result, reason, queued }</c>.</summary>
public sealed record RestCommandResult
{
    /// <summary>True when the vehicle or Tesla accepted the command.</summary>
    [JsonPropertyName("result")]
    public bool Result { get; init; }

    /// <summary>Failure reason or empty string.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>True when the command was queued for delivery (navigation share), if reported.</summary>
    [JsonPropertyName("queued")]
    public bool? Queued { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>The authenticated user's profile.</summary>
public sealed record UserProfile
{
    /// <summary>Account e-mail address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>Display name.</summary>
    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    /// <summary>URL of the profile image.</summary>
    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; init; }

    /// <summary>Tesla vault UUID of the user.</summary>
    [JsonPropertyName("vault_uuid")]
    public string? VaultUuid { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle order of the user (field names are camelCase on the wire).</summary>
public sealed record VehicleOrder
{
    /// <summary>Internal vehicle map id.</summary>
    [JsonPropertyName("vehicleMapId")]
    public long? VehicleMapId { get; init; }

    /// <summary>Order reference number (RN...).</summary>
    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; init; }

    /// <summary>VIN, once assigned.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Order status, e.g. <c>BOOKED</c>.</summary>
    [JsonPropertyName("orderStatus")]
    public string? OrderStatus { get; init; }

    /// <summary>Order sub-status code.</summary>
    [JsonPropertyName("orderSubstatus")]
    public string? OrderSubstatus { get; init; }

    /// <summary>Model code, e.g. <c>m3</c>.</summary>
    [JsonPropertyName("modelCode")]
    public string? ModelCode { get; init; }

    /// <summary>Country code.</summary>
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    /// <summary>Locale, e.g. <c>en_US</c>.</summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    /// <summary>Comma-separated option codes.</summary>
    [JsonPropertyName("mktOptions")]
    public string? MarketingOptions { get; init; }

    /// <summary>True for business orders.</summary>
    [JsonPropertyName("isB2b")]
    public bool? IsBusiness { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>The user's Fleet API region and base URL.</summary>
public sealed record UserRegion
{
    /// <summary>Region code, e.g. <c>na</c> or <c>eu</c>.</summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>Fleet API base URL for the region.</summary>
    [JsonPropertyName("fleet_api_base_url")]
    public string? FleetApiBaseUrl { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A page of business-fleet charging sessions.</summary>
public sealed record ChargingSessionsPage
{
    /// <summary>The sessions.</summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<FleetChargingSession>? Data { get; init; }

    /// <summary>Status code reported in the payload.</summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; init; }

    /// <summary>Status message reported in the payload.</summary>
    [JsonPropertyName("status_message")]
    public string? StatusMessage { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One charging session of a business fleet vehicle (shape from the docs example, unverified).</summary>
public sealed record FleetChargingSession
{
    /// <summary>Session id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>VIN.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Model name.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>Session start as reported (string).</summary>
    [JsonPropertyName("start_date_time")]
    public string? StartDateTime { get; init; }

    /// <summary>Session stop as reported (string).</summary>
    [JsonPropertyName("stop_date_time")]
    public string? StopDateTime { get; init; }

    /// <summary>Where the session took place.</summary>
    [JsonPropertyName("location")]
    public FleetChargingLocation? Location { get; init; }

    /// <summary>Charging periods with their dimensions.</summary>
    [JsonPropertyName("charging_periods")]
    public IReadOnlyList<FleetChargingPeriod>? ChargingPeriods { get; init; }

    /// <summary>Applied tariffs.</summary>
    [JsonPropertyName("tariffs")]
    public FleetChargingTariffs? Tariffs { get; init; }

    /// <summary>Total cost.</summary>
    [JsonPropertyName("total_cost")]
    public FleetChargingCost? TotalCost { get; init; }

    /// <summary>Total energy.</summary>
    [JsonPropertyName("total_energy")]
    public decimal? TotalEnergy { get; init; }

    /// <summary>Total time.</summary>
    [JsonPropertyName("total_time")]
    public decimal? TotalTime { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Location of a fleet charging session.</summary>
public sealed record FleetChargingLocation
{
    /// <summary>Country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    /// <summary>Site name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A charging period of a fleet charging session.</summary>
public sealed record FleetChargingPeriod
{
    /// <summary>Period start as reported (string).</summary>
    [JsonPropertyName("start_date_time")]
    public string? StartDateTime { get; init; }

    /// <summary>Measured dimensions.</summary>
    [JsonPropertyName("dimensions")]
    public IReadOnlyList<FleetChargingDimension>? Dimensions { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A measured dimension of a charging period.</summary>
public sealed record FleetChargingDimension
{
    /// <summary>Dimension type, e.g. <c>ENERGY</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Measured volume.</summary>
    [JsonPropertyName("volume")]
    public decimal? Volume { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Tariffs applied to a fleet charging session.</summary>
public sealed record FleetChargingTariffs
{
    /// <summary>ISO currency code.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    /// <summary>Tariff elements.</summary>
    [JsonPropertyName("elements")]
    public IReadOnlyList<FleetChargingTariffElement>? Elements { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One tariff element.</summary>
public sealed record FleetChargingTariffElement
{
    /// <summary>Price components.</summary>
    [JsonPropertyName("price_components")]
    public IReadOnlyList<FleetChargingPriceComponent>? PriceComponents { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One price component of a tariff element.</summary>
public sealed record FleetChargingPriceComponent
{
    /// <summary>Unit price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Step size.</summary>
    [JsonPropertyName("step_size")]
    public decimal? StepSize { get; init; }

    /// <summary>Component type, e.g. <c>ENERGY</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Total cost of a fleet charging session.</summary>
public sealed record FleetChargingCost
{
    /// <summary>Cost excluding VAT.</summary>
    [JsonPropertyName("excl_vat")]
    public decimal? ExclVat { get; init; }

    /// <summary>Cost including VAT.</summary>
    [JsonPropertyName("incl_vat")]
    public decimal? InclVat { get; init; }

    /// <summary>VAT amount.</summary>
    [JsonPropertyName("vat")]
    public decimal? Vat { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An option code of a vehicle.</summary>
public sealed record VehicleOption
{
    /// <summary>Option code, e.g. <c>$PPSW</c>.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Human-readable name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>Whether the option is active.</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    /// <summary>Color code for paint options.</summary>
    [JsonPropertyName("colorCode")]
    public string? ColorCode { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Active, upcoming and expired warranties.</summary>
public sealed record WarrantyDetails
{
    /// <summary>Currently active warranties.</summary>
    [JsonPropertyName("activeWarranty")]
    public IReadOnlyList<WarrantyCoverage>? ActiveWarranty { get; init; }

    /// <summary>Warranties that have not started yet.</summary>
    [JsonPropertyName("upcomingWarranty")]
    public IReadOnlyList<WarrantyCoverage>? UpcomingWarranty { get; init; }

    /// <summary>Expired warranties.</summary>
    [JsonPropertyName("expiredWarranty")]
    public IReadOnlyList<WarrantyCoverage>? ExpiredWarranty { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One warranty entry.</summary>
public sealed record WarrantyCoverage
{
    /// <summary>Type, e.g. <c>BATTERY_WARRANTY</c>.</summary>
    [JsonPropertyName("warrantyType")]
    public string? WarrantyType { get; init; }

    /// <summary>Display name.</summary>
    [JsonPropertyName("warrantyDisplayName")]
    public string? WarrantyDisplayName { get; init; }

    /// <summary>Expiration date.</summary>
    [JsonPropertyName("expirationDate")]
    public DateTimeOffset? ExpirationDate { get; init; }

    /// <summary>Expiration odometer in <see cref="OdometerUnit"/>.</summary>
    [JsonPropertyName("expirationOdometer")]
    public long? ExpirationOdometer { get; init; }

    /// <summary>Odometer unit, e.g. <c>MI</c> or <c>KM</c>.</summary>
    [JsonPropertyName("odometerUnit")]
    public string? OdometerUnit { get; init; }

    /// <summary>When the warranty expired, if it did.</summary>
    [JsonPropertyName("warrantyExpiredOn")]
    public DateTimeOffset? WarrantyExpiredOn { get; init; }

    /// <summary>Coverage age in years.</summary>
    [JsonPropertyName("coverageAgeInYears")]
    public int? CoverageAgeInYears { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A priced billing option of a subscription or add-on.</summary>
public sealed record BillingOption
{
    /// <summary>Billing period.</summary>
    [JsonPropertyName("billingPeriod")]
    public string? BillingPeriod { get; init; }

    /// <summary>ISO currency code.</summary>
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; init; }

    /// <summary>Option code.</summary>
    [JsonPropertyName("optionCode")]
    public string? OptionCode { get; init; }

    /// <summary>Net price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Tax.</summary>
    [JsonPropertyName("tax")]
    public decimal? Tax { get; init; }

    /// <summary>Gross total.</summary>
    [JsonPropertyName("total")]
    public decimal? Total { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A subscription a vehicle is eligible for.</summary>
public sealed record SubscriptionOffer
{
    /// <summary>Option code.</summary>
    [JsonPropertyName("optionCode")]
    public string? OptionCode { get; init; }

    /// <summary>Product name.</summary>
    [JsonPropertyName("product")]
    public string? Product { get; init; }

    /// <summary>Start date as reported.</summary>
    [JsonPropertyName("startDate")]
    public string? StartDate { get; init; }

    /// <summary>Add-ons.</summary>
    [JsonPropertyName("addons")]
    public IReadOnlyList<BillingOption>? Addons { get; init; }

    /// <summary>Billing options.</summary>
    [JsonPropertyName("billingOptions")]
    public IReadOnlyList<BillingOption>? BillingOptions { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Subscriptions a vehicle is eligible for.</summary>
public sealed record SubscriptionEligibility
{
    /// <summary>Country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    /// <summary>VIN.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Eligible subscriptions.</summary>
    [JsonPropertyName("eligible")]
    public IReadOnlyList<SubscriptionOffer>? Eligible { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A price of an upgrade.</summary>
public sealed record UpgradePrice
{
    /// <summary>Net price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Gross total.</summary>
    [JsonPropertyName("total")]
    public decimal? Total { get; init; }

    /// <summary>ISO currency code.</summary>
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; init; }

    /// <summary>Whether this is the primary price.</summary>
    [JsonPropertyName("isPrimary")]
    public bool? IsPrimary { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An upgrade a vehicle is eligible for.</summary>
public sealed record UpgradeOffer
{
    /// <summary>Option code of the upgrade.</summary>
    [JsonPropertyName("optionCode")]
    public string? OptionCode { get; init; }

    /// <summary>Option group.</summary>
    [JsonPropertyName("optionGroup")]
    public string? OptionGroup { get; init; }

    /// <summary>Option code the vehicle has now.</summary>
    [JsonPropertyName("currentOptionCode")]
    public string? CurrentOptionCode { get; init; }

    /// <summary>Prices.</summary>
    [JsonPropertyName("pricing")]
    public IReadOnlyList<UpgradePrice>? Pricing { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Upgrades a vehicle is eligible for.</summary>
public sealed record UpgradeEligibility
{
    /// <summary>VIN.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    /// <summary>Upgrade type, e.g. <c>VEHICLE</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Eligible upgrades.</summary>
    [JsonPropertyName("eligible")]
    public IReadOnlyList<UpgradeOffer>? Eligible { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An owner, driver or payer of a business vehicle.</summary>
public sealed record EnterpriseEntity
{
    /// <summary><c>BUSINESS</c> or <c>PERSON</c>.</summary>
    [JsonPropertyName("entity_type")]
    public string? EntityType { get; init; }

    /// <summary>Business account id.</summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; init; }

    /// <summary>Company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Federation id of a person.</summary>
    [JsonPropertyName("federation_id")]
    public string? FederationId { get; init; }

    /// <summary>E-mail of a person.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Who pays for what on a business vehicle.</summary>
public sealed record EnterprisePayers
{
    /// <summary>Payer of subscriptions.</summary>
    [JsonPropertyName("subscription")]
    public EnterpriseEntity? Subscription { get; init; }

    /// <summary>Payer of charging.</summary>
    [JsonPropertyName("charging")]
    public EnterpriseEntity? Charging { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Owner, drivers and payers of a business vehicle.</summary>
public sealed record EnterpriseRoles
{
    /// <summary>VIN.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>The owner.</summary>
    [JsonPropertyName("owner")]
    public EnterpriseEntity? Owner { get; init; }

    /// <summary>The drivers.</summary>
    [JsonPropertyName("drivers")]
    public IReadOnlyList<EnterpriseEntity>? Drivers { get; init; }

    /// <summary>The payers.</summary>
    [JsonPropertyName("payers")]
    public EnterprisePayers? Payers { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A priced equipment item of a vehicle.</summary>
public sealed record SpecsEquipmentItem
{
    /// <summary>Option code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Vehicle specifications (partner token, billed per result). Battery fields may be null.</summary>
public sealed record VehicleSpecs
{
    /// <summary>VIN.</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Battery state of health, e.g. <c>100%</c>.</summary>
    [JsonPropertyName("batterySoH")]
    public string? BatterySoH { get; init; }

    /// <summary>Usable pack energy, e.g. <c>82.3kWh</c>.</summary>
    [JsonPropertyName("batteryCapacityKwh")]
    public string? BatteryCapacityKwh { get; init; }

    /// <summary>Time of the battery estimate.</summary>
    [JsonPropertyName("batterySoHTimestamp")]
    public DateTimeOffset? BatterySoHTimestamp { get; init; }

    /// <summary>Make.</summary>
    [JsonPropertyName("make")]
    public string? Make { get; init; }

    /// <summary>Model code.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>Model year.</summary>
    [JsonPropertyName("modelYear")]
    public int? ModelYear { get; init; }

    /// <summary>Body type.</summary>
    [JsonPropertyName("bodyType")]
    public string? BodyType { get; init; }

    /// <summary>Equipment and prices.</summary>
    [JsonPropertyName("equipmentPrice")]
    public IReadOnlyList<SpecsEquipmentItem>? EquipmentPrice { get; init; }

    /// <summary>Trim.</summary>
    [JsonPropertyName("trim")]
    public string? Trim { get; init; }

    /// <summary>Drive type, e.g. <c>AWD</c>.</summary>
    [JsonPropertyName("typeOfDrive")]
    public string? TypeOfDrive { get; init; }

    /// <summary>Seats.</summary>
    [JsonPropertyName("numberOfSeats")]
    public int? NumberOfSeats { get; init; }

    /// <summary>Doors.</summary>
    [JsonPropertyName("numberOfDoors")]
    public int? NumberOfDoors { get; init; }

    /// <summary>Paint.</summary>
    [JsonPropertyName("paintCode")]
    public string? PaintCode { get; init; }

    /// <summary>Interior.</summary>
    [JsonPropertyName("interiorCode")]
    public string? InteriorCode { get; init; }

    /// <summary>Third row seats.</summary>
    [JsonPropertyName("thirdRowSeatsCode")]
    public string? ThirdRowSeatsCode { get; init; }

    /// <summary>Decor.</summary>
    [JsonPropertyName("decorCode")]
    public string? DecorCode { get; init; }

    /// <summary>Wheels.</summary>
    [JsonPropertyName("wheelsCode")]
    public string? WheelsCode { get; init; }

    /// <summary>Rear seats.</summary>
    [JsonPropertyName("rearSeatsName")]
    public string? RearSeatsName { get; init; }

    /// <summary>Seats.</summary>
    [JsonPropertyName("seatsName")]
    public string? SeatsName { get; init; }

    /// <summary>Towing.</summary>
    [JsonPropertyName("towingCode")]
    public string? TowingCode { get; init; }

    /// <summary>Total price.</summary>
    [JsonPropertyName("totalPrice")]
    public decimal? TotalPrice { get; init; }

    /// <summary>Vehicle type.</summary>
    [JsonPropertyName("vehicleType")]
    public string? VehicleType { get; init; }

    /// <summary>Fuel type.</summary>
    [JsonPropertyName("fuelType")]
    public string? FuelType { get; init; }

    /// <summary>Transmission.</summary>
    [JsonPropertyName("transmissionType")]
    public string? TransmissionType { get; init; }

    /// <summary>Gears.</summary>
    [JsonPropertyName("numberOfGears")]
    public int? NumberOfGears { get; init; }

    /// <summary>Autopilot hardware.</summary>
    [JsonPropertyName("autopilotHardwareCode")]
    public string? AutopilotHardwareCode { get; init; }

    /// <summary>Autopilot software.</summary>
    [JsonPropertyName("autopilotSoftwareCode")]
    public string? AutopilotSoftwareCode { get; init; }

    /// <summary>Country of origin.</summary>
    [JsonPropertyName("countryOfOrigin")]
    public string? CountryOfOrigin { get; init; }

    /// <summary>Production date.</summary>
    [JsonPropertyName("productionDate")]
    public DateTimeOffset? ProductionDate { get; init; }

    /// <summary>Max power, formatted.</summary>
    [JsonPropertyName("maxPowerInKw")]
    public string? MaxPowerInKw { get; init; }

    /// <summary>Battery range, formatted.</summary>
    [JsonPropertyName("batteryRangeInKm")]
    public string? BatteryRangeInKm { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Variant of a pricing configuration.</summary>
public sealed record PricingVariant
{
    /// <summary>Variant code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Variant name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A priced option of a configuration.</summary>
public sealed record PricingOption
{
    /// <summary>Option code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Group.</summary>
    [JsonPropertyName("group")]
    public string? Group { get; init; }

    /// <summary>Price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A warranty option of a configuration.</summary>
public sealed record PricingWarrantyOption
{
    /// <summary>Code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Group.</summary>
    [JsonPropertyName("group")]
    public string? Group { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Warranty information of a configuration.</summary>
public sealed record PricingWarranty
{
    /// <summary>Whether prices include tax.</summary>
    [JsonPropertyName("isTaxInclusive")]
    public bool? IsTaxInclusive { get; init; }

    /// <summary>Warranty options.</summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<PricingWarrantyOption>? Options { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Headline specs of a configuration (formatted strings).</summary>
public sealed record PricingSpecs
{
    /// <summary>Model name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Range.</summary>
    [JsonPropertyName("range_formatted")]
    public string? RangeFormatted { get; init; }

    /// <summary>Top speed.</summary>
    [JsonPropertyName("topspeed_formatted")]
    public string? TopSpeedFormatted { get; init; }

    /// <summary>Acceleration.</summary>
    [JsonPropertyName("acceleration_formatted")]
    public string? AccelerationFormatted { get; init; }

    /// <summary>Adult seats.</summary>
    [JsonPropertyName("seating_capacity_adults")]
    public int? SeatingCapacityAdults { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One priced configuration of a model.</summary>
public sealed record PricingConfiguration
{
    /// <summary>Variant.</summary>
    [JsonPropertyName("variant")]
    public PricingVariant? Variant { get; init; }

    /// <summary>ISO currency code.</summary>
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; init; }

    /// <summary>Currency symbol.</summary>
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol { get; init; }

    /// <summary>Subtotal.</summary>
    [JsonPropertyName("subtotal")]
    public decimal? Subtotal { get; init; }

    /// <summary>Order fee.</summary>
    [JsonPropertyName("orderFee")]
    public decimal? OrderFee { get; init; }

    /// <summary>Delivery fee.</summary>
    [JsonPropertyName("deliveryFee")]
    public decimal? DeliveryFee { get; init; }

    /// <summary>Whether VAT applies.</summary>
    [JsonPropertyName("vat")]
    public bool? Vat { get; init; }

    /// <summary>Options.</summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<PricingOption>? Options { get; init; }

    /// <summary>Warranty.</summary>
    [JsonPropertyName("warranty")]
    public PricingWarranty? Warranty { get; init; }

    /// <summary>Specs.</summary>
    [JsonPropertyName("specs")]
    public PricingSpecs? Specs { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An accessory with its price.</summary>
public sealed record PricingAccessory
{
    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Price.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Model configuration prices for a market (partner token).</summary>
public sealed record VehiclePricing
{
    /// <summary>Configurations.</summary>
    [JsonPropertyName("configs")]
    public IReadOnlyList<PricingConfiguration>? Configs { get; init; }

    /// <summary>Accessories.</summary>
    [JsonPropertyName("accessories")]
    public IReadOnlyList<PricingAccessory>? Accessories { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Where a navigation destination goes in the current trip.</summary>
public enum NavigationTripOrder
{
    /// <summary>Replace the current destination.</summary>
    Replace = 0,

    /// <summary>Insert before the current destination.</summary>
    Prepend = 1,

    /// <summary>Append after the current destination.</summary>
    Append = 2,
}

/// <summary>Filters and paging for <see cref="ICoilProClient.GetChargingHistoryAsync"/> (all optional).</summary>
public sealed record ChargingHistoryQuery
{
    /// <summary>Only sessions of this VIN.</summary>
    public string? Vin { get; init; }

    /// <summary>Sessions starting at or after this time.</summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>Sessions starting before this time.</summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>Page number (Tesla <c>pageNo</c>).</summary>
    public int? PageNumber { get; init; }

    /// <summary>Page size (Tesla <c>pageSize</c>).</summary>
    public int? PageSize { get; init; }

    /// <summary>Field to sort by (Tesla <c>sortBy</c>).</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort ascending (true) or descending (false).</summary>
    public bool? Ascending { get; init; }
}

/// <summary>Filters and paging for <see cref="ICoilProClient.GetChargingSessionsAsync"/> (all optional).</summary>
public sealed record ChargingSessionsQuery
{
    /// <summary>Only sessions of this VIN.</summary>
    public string? Vin { get; init; }

    /// <summary>Sessions from this date.</summary>
    public DateTimeOffset? DateFrom { get; init; }

    /// <summary>Sessions up to this date.</summary>
    public DateTimeOffset? DateTo { get; init; }

    /// <summary>Maximum number of sessions.</summary>
    public int? Limit { get; init; }

    /// <summary>Number of sessions to skip.</summary>
    public int? Offset { get; init; }
}

/// <summary>Which bill a business payer covers.</summary>
public enum EnterprisePayerRole
{
    /// <summary>Supercharging costs.</summary>
    Charging,

    /// <summary>Connectivity and other subscriptions.</summary>
    Subscription,
}

/// <summary>Request for <see cref="ICoilProClient.SetEnterprisePayerAsync"/>: set exactly one of <see cref="AccountId"/> or <see cref="FederationId"/>.</summary>
public sealed record EnterprisePayerRequest
{
    /// <summary>Which bill the payer covers.</summary>
    public required EnterprisePayerRole Role { get; init; }

    /// <summary>Business account id of the payer.</summary>
    public string? AccountId { get; init; }

    /// <summary>Federation id of a driver as payer.</summary>
    public string? FederationId { get; init; }
}
