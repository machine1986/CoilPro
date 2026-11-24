using System.Text.Json.Serialization;

namespace CoilPro.Internal;

/// <summary>
/// The Fleet API wraps payloads in a <c>{ "response": ... }</c> envelope (list endpoints add <c>count</c>).
/// </summary>
internal sealed record TeslaResponse<T>
{
    [JsonPropertyName("response")]
    public T? Response { get; init; }

    [JsonPropertyName("count")]
    public int? Count { get; init; }
}
