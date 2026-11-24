namespace CoilPro.Cli.Internal;

/// <summary>Prints a <see cref="RateLimitInfo"/> snapshot in a consistent, human-readable way.</summary>
internal static class RateLimitPrinter
{
    /// <summary>Writes the rate-limit snapshot to stdout, or does nothing if <paramref name="rateLimit"/> is null.</summary>
    public static void Print(RateLimitInfo? rateLimit)
    {
        if (rateLimit is null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Rate limit:");

        if (rateLimit.Remaining is not null || rateLimit.Limit is not null)
        {
            Console.WriteLine($"  Remaining: {rateLimit.Remaining?.ToString() ?? "?"} / {rateLimit.Limit?.ToString() ?? "?"}");
        }

        if (rateLimit.ResetsAt is not null)
        {
            Console.WriteLine($"  Resets at: {rateLimit.ResetsAt:u}");
        }

        if (rateLimit.RetryAfter is not null)
        {
            Console.WriteLine($"  Retry after: {rateLimit.RetryAfter}");
        }
    }
}
