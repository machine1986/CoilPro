using Coil.Cli.Internal;

namespace Coil.Cli.Commands;

/// <summary>
/// Implements <c>coil login</c>: runs the Tesla OAuth authorization-code + PKCE flow end to end using a local
/// loopback redirect (RFC 8252), and prints a masked summary of the resulting tokens. Tokens are never written
/// to stdout/logs in full, and are not persisted anywhere by the CLI itself.
/// </summary>
internal static class LoginCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine($"coil login: {ex.Message}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            Console.Error.WriteLine("coil login: missing client id. Pass --client-id or set COIL_CLIENT_ID.");
            return 1;
        }

        using var listener = new LoopbackListener();
        Uri redirectUri;
        try
        {
            redirectUri = listener.Start(options.Port);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"coil login: could not start the local loopback listener: {ex.Message}");
            return 1;
        }

        var authOptions = new TeslaAuthOptions
        {
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret,
            Region = options.Region,
            Scopes = options.Scopes,
            RedirectUri = redirectUri,
        };

        using var http = new HttpClient();
        ITeslaAuthClient authClient = new TeslaAuthClient(http, authOptions);

        AuthorizationRequest authorizationRequest;
        try
        {
            authorizationRequest = authClient.CreateAuthorizationRequest();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"coil login: could not build the authorization request: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"Region: {options.Region}");
        Console.WriteLine($"Redirect URI: {redirectUri}");
        Console.WriteLine();
        Console.WriteLine("Opening your browser to sign in with Tesla...");
        Console.WriteLine("If it doesn't open automatically, visit:");
        Console.WriteLine($"  {authorizationRequest.AuthorizationUrl}");
        Console.WriteLine();

        if (!BrowserLauncher.TryOpen(authorizationRequest.AuthorizationUrl))
        {
            Console.WriteLine("(Could not launch a browser automatically — open the URL above manually.)");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(options.Timeout);

        LoopbackCallbackResult callback;
        try
        {
            callback = await listener.WaitForCallbackAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coil login: cancelled.");
            return 130;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine($"coil login: timed out after {options.Timeout.TotalSeconds:0}s waiting for the browser callback.");
            return 1;
        }

        if (!string.Equals(callback.State, authorizationRequest.State, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("coil login: state mismatch on callback — aborting (possible CSRF/stale request).");
            return 1;
        }

        if (callback.Error is not null)
        {
            Console.Error.WriteLine($"coil login: authorization failed: {callback.Error}" +
                (callback.ErrorDescription is null ? string.Empty : $" — {callback.ErrorDescription}"));
            return 1;
        }

        if (string.IsNullOrEmpty(callback.Code))
        {
            Console.Error.WriteLine("coil login: no authorization code was returned.");
            return 1;
        }

        TeslaTokens tokens;
        try
        {
            tokens = await authClient.ExchangeCodeAsync(callback.Code, authorizationRequest.Pkce.Verifier, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"coil login: token exchange failed: {ex.Message}");
            return 1;
        }

        PrintSuccess(tokens);
        return 0;
    }

    private static void PrintSuccess(TeslaTokens tokens)
    {
        Console.WriteLine("Login succeeded.");
        Console.WriteLine($"  Token type:    {tokens.TokenType}");
        Console.WriteLine($"  Expires at:    {tokens.ExpiresAt:u}");
        Console.WriteLine($"  Access token:  {TokenMask.Mask(tokens.AccessToken)}");
        Console.WriteLine($"  Refresh token: {TokenMask.Mask(tokens.RefreshToken)}");
        Console.WriteLine($"  ID token:      {TokenMask.Mask(tokens.IdToken)}");
        Console.WriteLine();
        Console.WriteLine("Tokens were not written to disk or logs. Wire up an ITokenStore in your app to persist them.");
    }
}
