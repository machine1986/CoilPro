using System.Globalization;
using CoilPro.Cli.Commands;

namespace CoilPro.Cli.Internal;

/// <summary>
/// Shared plumbing for the read commands: argument parsing, session loading, client creation and uniform
/// error-to-exit-code mapping (0 ok, 1 error, 130 cancelled).
/// </summary>
internal static class CliRunner
{
    /// <summary>Parsed arguments: positionals plus <c>--name value</c> options and boolean flags.</summary>
    internal sealed class ParsedArgs
    {
        public List<string> Positional { get; } = [];

        public Dictionary<string, string> Options { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Flags { get; } = new(StringComparer.Ordinal);

        public bool HelpRequested { get; set; }

        public string? Option(string name) => Options.GetValueOrDefault(name);

        public bool Flag(string name) => Flags.Contains(name);

        public int? IntOption(string name)
        {
            var value = Option(name);
            if (value is null)
            {
                return null;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n
                : throw new CliUsageException($"invalid {name} '{value}' (expected an integer).");
        }

        public DateTimeOffset? DateOption(string name)
        {
            var value = Option(name);
            if (value is null)
            {
                return null;
            }

            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)
                ? d
                : throw new CliUsageException($"invalid {name} '{value}' (expected e.g. 2026-11-01 or 2026-11-01T08:00:00Z).");
        }
    }

    /// <summary>Splits <paramref name="args"/> into positionals and the given value options / flags. Supports <c>--name=value</c>.</summary>
    public static ParsedArgs Parse(string[] args, string[]? valueOptions = null, string[]? flags = null)
    {
        var result = new ParsedArgs();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--help" or "-h" or "-?")
            {
                result.HelpRequested = true;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                result.Positional.Add(arg);
                continue;
            }

            var eq = arg.IndexOf('=');
            var name = eq < 0 ? arg : arg[..eq];
            if (flags?.Contains(name) == true)
            {
                result.Flags.Add(name);
            }
            else if (valueOptions?.Contains(name) == true)
            {
                if (eq >= 0)
                {
                    result.Options[name] = arg[(eq + 1)..];
                }
                else if (i + 1 < args.Length)
                {
                    result.Options[name] = args[++i];
                }
                else
                {
                    throw new CliUsageException($"missing value for {name}.");
                }
            }
            else
            {
                throw new CliUsageException($"unknown option '{arg}'.");
            }
        }

        return result;
    }

    /// <summary>Runs a read command: handles <c>--help</c>, parse errors, the login check and API errors.</summary>
    public static async Task<int> RunAsync(
        string command,
        string[] args,
        string usage,
        string[]? valueOptions,
        string[]? flags,
        CancellationToken ct,
        Func<ParsedArgs, CliClientFactory.CliClients, Task<int>> body,
        Func<ParsedArgs, string?>? validate = null)
    {
        ParsedArgs parsed;
        try
        {
            parsed = Parse(args, valueOptions, flags);
            if (parsed.HelpRequested)
            {
                Console.WriteLine(usage);
                return 0;
            }

            var problem = validate?.Invoke(parsed);
            if (problem is not null)
            {
                throw new CliUsageException(problem);
            }
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine($"coilpro {command}: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(usage);
            return 1;
        }

        var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
        if (session is null || tokens is null)
        {
            VehiclesCommand.PrintNotLoggedIn($"coilpro {command}");
            return 1;
        }

        using var clients = CliClientFactory.Create(session);
        try
        {
            return await body(parsed, clients).ConfigureAwait(false);
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine($"coilpro {command}: {ex.Message}");
            return 1;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro {command}: {VehiclesCommand.DescribeApiError(ex)}");
            return 1;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"coilpro {command}: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine($"coilpro {command}: cancelled.");
            return 130;
        }
    }

    /// <summary>Formats a value for table output; null/empty becomes "-".</summary>
    public static string Dash(object? value) => value switch
    {
        null => "-",
        string s when string.IsNullOrWhiteSpace(s) => "-",
        bool b => b ? "yes" : "no",
        DateTimeOffset d => d.ToString("u", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "-",
    };
}
