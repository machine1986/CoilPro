namespace CoilPro.Commands;

/// <summary>Heated seat positions (<c>remote_seat_heater_request</c> <c>seat_position</c> index in brackets).</summary>
public enum SeatPosition
{
    /// <summary>Front left [0].</summary>
    FrontLeft,
    /// <summary>Front right [1].</summary>
    FrontRight,
    /// <summary>Rear left [2].</summary>
    RearLeft,
    /// <summary>Rear left backrest [3] (some Model S).</summary>
    RearLeftBack,
    /// <summary>Rear center [4].</summary>
    RearCenter,
    /// <summary>Rear right [5].</summary>
    RearRight,
    /// <summary>Rear right backrest [6] (some Model S).</summary>
    RearRightBack,
    /// <summary>Third row left [7].</summary>
    ThirdRowLeft,
    /// <summary>Third row right [8].</summary>
    ThirdRowRight,
}

/// <summary>Seat heater level.</summary>
public enum SeatHeaterLevel
{
    /// <summary>Off.</summary>
    Off,
    /// <summary>Low.</summary>
    Low,
    /// <summary>Medium.</summary>
    Medium,
    /// <summary>High.</summary>
    High,
}

/// <summary>Front seats with cooling or auto climate.</summary>
public enum FrontSeat
{
    /// <summary>Front left.</summary>
    FrontLeft,
    /// <summary>Front right.</summary>
    FrontRight,
}

/// <summary>Seat cooler (ventilation) level.</summary>
public enum SeatCoolerLevel
{
    /// <summary>Off.</summary>
    Off,
    /// <summary>Low.</summary>
    Low,
    /// <summary>Medium.</summary>
    Medium,
    /// <summary>High.</summary>
    High,
}

/// <summary>Cabin overheat protection activation temperature (about 30 / 35 / 40 °C).</summary>
public enum CabinOverheatTemperature
{
    /// <summary>Low, about 30 °C / 90 °F.</summary>
    Low,
    /// <summary>Medium, about 35 °C / 95 °F.</summary>
    Medium,
    /// <summary>High, about 40 °C / 100 °F.</summary>
    High,
}

/// <summary>Climate keeper mode.</summary>
public enum ClimateKeeperMode
{
    /// <summary>Off.</summary>
    Off,
    /// <summary>Keep climate on.</summary>
    Keep,
    /// <summary>Dog mode.</summary>
    Dog,
    /// <summary>Camp mode.</summary>
    Camp,
}

/// <summary>Parental controls policy settings.</summary>
public enum ParentalControlsSetting
{
    /// <summary>Speed limit.</summary>
    SpeedLimit,
    /// <summary>Chill acceleration.</summary>
    Acceleration,
    /// <summary>Safety features always on.</summary>
    SafetyFeatures,
    /// <summary>Driving curfew.</summary>
    Curfew,
    /// <summary>Browser blocked.</summary>
    BrowserBlocked,
    /// <summary>Theater blocked.</summary>
    TheaterBlocked,
    /// <summary>Arcade blocked.</summary>
    ArcadeBlocked,
}

/// <summary>Days of the week for charge and precondition schedules (Tesla bitmask: Sunday = 1 … Saturday = 64).</summary>
[Flags]
public enum ScheduleDays
{
    /// <summary>No day.</summary>
    None = 0,
    /// <summary>Sunday.</summary>
    Sunday = 1,
    /// <summary>Monday.</summary>
    Monday = 2,
    /// <summary>Tuesday.</summary>
    Tuesday = 4,
    /// <summary>Wednesday.</summary>
    Wednesday = 8,
    /// <summary>Thursday.</summary>
    Thursday = 16,
    /// <summary>Friday.</summary>
    Friday = 32,
    /// <summary>Saturday.</summary>
    Saturday = 64,
    /// <summary>Monday to Friday.</summary>
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    /// <summary>Every day.</summary>
    All = Weekdays | Saturday | Sunday,
}

/// <summary>Which schedules a batch removal affects.</summary>
[Flags]
public enum ScheduleLocations
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Schedules at home.</summary>
    Home = 1,
    /// <summary>Schedules at work.</summary>
    Work = 2,
    /// <summary>All other schedules.</summary>
    Other = 4,
}

/// <summary>A location-bound charge schedule (<c>add_charge_schedule</c>, firmware 2024.26+).</summary>
public sealed record ChargeSchedule
{
    /// <summary>Client-chosen id (Tesla uses Unix seconds); reuse it to replace or remove the schedule.</summary>
    public required ulong Id { get; init; }

    /// <summary>Days the schedule applies to.</summary>
    public required ScheduleDays Days { get; init; }

    /// <summary>Start charging at this time; null = no start time.</summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>Stop charging at this time; null = no end time.</summary>
    public TimeOnly? EndTime { get; init; }

    /// <summary>Run once instead of repeating.</summary>
    public bool OneTime { get; init; }

    /// <summary>Whether the schedule is active.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Latitude of the location the schedule is bound to.</summary>
    public required float Latitude { get; init; }

    /// <summary>Longitude of the location the schedule is bound to.</summary>
    public required float Longitude { get; init; }
}

/// <summary>A location-bound preconditioning schedule (<c>add_precondition_schedule</c>).</summary>
public sealed record PreconditionSchedule
{
    /// <summary>Client-chosen id (Tesla uses Unix seconds); reuse it to replace or remove the schedule.</summary>
    public required ulong Id { get; init; }

    /// <summary>Days the schedule applies to.</summary>
    public required ScheduleDays Days { get; init; }

    /// <summary>Time the cabin should be ready.</summary>
    public required TimeOnly PreconditionTime { get; init; }

    /// <summary>Run once instead of repeating.</summary>
    public bool OneTime { get; init; }

    /// <summary>Whether the schedule is active.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Latitude of the location the schedule is bound to.</summary>
    public required float Latitude { get; init; }

    /// <summary>Longitude of the location the schedule is bound to.</summary>
    public required float Longitude { get; init; }
}

/// <summary>Weekly policy for legacy scheduled departure preconditioning / off-peak charging.</summary>
public enum DeparturePolicy
{
    /// <summary>Disabled.</summary>
    Off,
    /// <summary>Every day.</summary>
    AllWeek,
    /// <summary>Weekdays only.</summary>
    WeekdaysOnly,
}

/// <summary>Legacy scheduled departure (<c>set_scheduled_departure</c>; Tesla recommends charge/precondition schedules from firmware 2024.26).</summary>
public sealed record ScheduledDeparture
{
    /// <summary>Departure time.</summary>
    public required TimeOnly DepartureTime { get; init; }

    /// <summary>Preconditioning policy.</summary>
    public DeparturePolicy Preconditioning { get; init; } = DeparturePolicy.Off;

    /// <summary>Off-peak charging policy.</summary>
    public DeparturePolicy OffPeakCharging { get; init; } = DeparturePolicy.Off;

    /// <summary>End of off-peak hours.</summary>
    public TimeOnly? OffPeakHoursEndTime { get; init; }
}
