namespace Coil.Internal;

/// <summary>Minimal base64url (RFC 4648 §5) helper — no padding, URL-safe alphabet.</summary>
internal static class Base64Url
{
    /// <summary>Encodes <paramref name="bytes"/> as unpadded base64url.</summary>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
