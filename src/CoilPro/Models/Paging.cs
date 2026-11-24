using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>
/// Top-level <c>pagination</c> object of Fleet API list endpoints (sits next to <c>response</c>, not inside it).
/// </summary>
public sealed record TeslaPagination
{
    /// <summary>Previous page number, if any.</summary>
    [JsonPropertyName("previous")] public int? Previous { get; init; }

    /// <summary>Next page number; null on the last page.</summary>
    [JsonPropertyName("next")] public int? Next { get; init; }

    /// <summary>Current page number.</summary>
    [JsonPropertyName("current")] public int? Current { get; init; }

    /// <summary>Page size used by the server.</summary>
    [JsonPropertyName("per_page")] public int? PerPage { get; init; }

    /// <summary>Total item count, if reported (often null).</summary>
    [JsonPropertyName("count")] public int? Count { get; init; }

    /// <summary>Total page count, if reported (often null).</summary>
    [JsonPropertyName("pages")] public int? Pages { get; init; }
}

/// <summary>One page of a Fleet API list endpoint.</summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed record ResultPage<T>
{
    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Paging metadata, if the endpoint returns it.</summary>
    public TeslaPagination? Pagination { get; init; }

    /// <summary>Top-level <c>count</c>, if the endpoint returns it.</summary>
    public int? Count { get; init; }
}
