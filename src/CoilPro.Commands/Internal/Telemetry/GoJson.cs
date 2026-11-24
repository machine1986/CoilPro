using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CoilPro.Commands.Internal.Telemetry;

/// <summary>
/// Writes JSON the way Go's <c>encoding/json</c> marshals a <c>map[string]interface{}</c> (which is what
/// <c>jwt.MapClaims</c> is): object keys sorted by their UTF-8 bytes, no whitespace, and Go's string escaping
/// (<c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>, U+2028 and U+2029 as <c>\uXXXX</c>; <c>+</c>, <c>/</c>, <c>=</c> and non-ASCII
/// verbatim). Emitting the same bytes as Tesla's proxy keeps our tokens byte-identical to the reference.
/// </summary>
/// <remarks>
/// Numbers are written as their JSON source text. Go would round-trip them through <c>float64</c> (e.g.
/// <c>1.50</c> → <c>1.5</c>); our input comes from <see cref="JsonSerializer"/>, which already emits shortest
/// forms, so the two only differ for exotic values. The signature covers whatever bytes are emitted, so this
/// never affects validity.
/// </remarks>
internal static class GoJson
{
    /// <summary>Serializes <paramref name="element"/> in Go's canonical map-marshalling form.</summary>
    public static string Serialize(JsonElement element)
    {
        var sb = new StringBuilder();
        Write(sb, element);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Duplicate keys: last one wins, as with Go's json.Unmarshal into a map.
                var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    properties[property.Name] = property.Value;
                }

                var keys = properties.Keys.ToArray();
                Array.Sort(keys, static (x, y) => Encoding.UTF8.GetBytes(x).AsSpan().SequenceCompareTo(Encoding.UTF8.GetBytes(y)));

                sb.Append('{');
                for (var i = 0; i < keys.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    WriteString(sb, keys[i]);
                    sb.Append(':');
                    Write(sb, properties[keys[i]]);
                }

                sb.Append('}');
                break;

            case JsonValueKind.Array:
                sb.Append('[');
                var first = true;
                foreach (var item in element.EnumerateArray())
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    Write(sb, item);
                }

                sb.Append(']');
                break;

            case JsonValueKind.String:
                WriteString(sb, element.GetString()!);
                break;

            case JsonValueKind.Number:
                sb.Append(element.GetRawText());
                break;

            case JsonValueKind.True:
                sb.Append("true");
                break;

            case JsonValueKind.False:
                sb.Append("false");
                break;

            case JsonValueKind.Null:
                sb.Append("null");
                break;

            default:
                throw new ArgumentException($"Unsupported JSON value kind {element.ValueKind}.", nameof(element));
        }
    }

    /// <summary>Go <c>encoding/json</c> string escaping (Go ≥ 1.22, HTML escaping on — the <c>json.Marshal</c> default).</summary>
    internal static void WriteString(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var rune in value.EnumerateRunes())
        {
            switch (rune.Value)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '<' or '>' or '&' or 0x2028 or 0x2029:
                    sb.Append("\\u").Append(rune.Value.ToString("x4", CultureInfo.InvariantCulture));
                    break;
                case < 0x20:
                    sb.Append("\\u").Append(rune.Value.ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default:
                    // EnumerateRunes yields U+FFFD for lone surrogates — the same replacement Go applies to invalid UTF-8.
                    sb.Append(rune.ToString());
                    break;
            }
        }

        sb.Append('"');
    }
}
