using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Coil.Cli.Internal;

/// <summary>The relevant query parameters from the OAuth redirect hitting the loopback listener.</summary>
internal sealed record LoopbackCallbackResult
{
    public string? Code { get; init; }

    public string? State { get; init; }

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }
}

/// <summary>
/// A minimal, single-use <c>http://127.0.0.1:&lt;port&gt;/callback</c> listener used to receive the OAuth
/// authorization-code redirect for <c>coil login</c> (RFC 8252 "loopback interface redirection").
/// </summary>
internal sealed class LoopbackListener : IDisposable
{
    private HttpListener? _listener;

    /// <summary>Starts the listener and returns the redirect URI to use as <see cref="TeslaAuthOptions.RedirectUri"/>.</summary>
    /// <param name="preferredPort">A fixed port to bind, or 0 to pick a free ephemeral port.</param>
    public Uri Start(int preferredPort = 0)
    {
        var port = preferredPort > 0 ? preferredPort : GetFreeTcpPort();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();

        return new Uri($"http://127.0.0.1:{port}/callback");
    }

    /// <summary>
    /// Waits for a request to <c>/callback</c> (ignoring stray requests such as favicon fetches), responds with a
    /// short human-readable page, and returns the parsed query parameters.
    /// </summary>
    public async Task<LoopbackCallbackResult> WaitForCallbackAsync(CancellationToken ct)
    {
        if (_listener is null)
        {
            throw new InvalidOperationException("Call Start() before WaitForCallbackAsync().");
        }

        var listener = _listener;

        await using var registration = ct.Register(static state => ((HttpListener)state!).Stop(), listener);

        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Timed out waiting for the OAuth callback.", ct);
            }

            if (context.Request.Url?.AbsolutePath != "/callback")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                continue;
            }

            var query = context.Request.Url.Query;
            var result = ParseQuery(query);

            var body = result.Error is null
                ? "<html><body><h3>Login successful.</h3>You can close this tab and return to the terminal.</body></html>"
                : $"<html><body><h3>Login failed.</h3><p>{WebUtility.HtmlEncode(result.Error)}</p></body></html>";

            var buffer = Encoding.UTF8.GetBytes(body);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            context.Response.Close();

            return result;
        }
    }

    private static LoopbackCallbackResult ParseQuery(string query)
    {
        string? code = null, state = null, error = null, errorDescription = null;

        foreach (var rawPair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = rawPair.IndexOf('=');
            var key = eq < 0 ? rawPair : rawPair[..eq];
            var value = eq < 0 ? string.Empty : Uri.UnescapeDataString(rawPair[(eq + 1)..].Replace('+', ' '));

            switch (key)
            {
                case "code": code = value; break;
                case "state": state = value; break;
                case "error": error = value; break;
                case "error_description": errorDescription = value; break;
            }
        }

        return new LoopbackCallbackResult { Code = code, State = state, Error = error, ErrorDescription = errorDescription };
    }

    private static int GetFreeTcpPort()
    {
        var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        try
        {
            return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        }
        finally
        {
            tcpListener.Stop();
        }
    }

    public void Dispose()
    {
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // best-effort cleanup
        }

        try
        {
            _listener?.Close();
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
