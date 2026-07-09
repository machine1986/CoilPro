namespace Coil.Cli.Internal;

/// <summary>Masks secrets for safe console output. The CLI must never print full tokens.</summary>
internal static class TokenMask
{
    /// <summary>Returns a masked form such as <c>abcd...wxyz (147 chars)</c>, safe to print or log.</summary>
    public static string Mask(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return "(none)";
        }

        if (secret.Length <= 8)
        {
            return new string('*', secret.Length);
        }

        return $"{secret[..4]}...{secret[^4..]} ({secret.Length} chars)";
    }
}
