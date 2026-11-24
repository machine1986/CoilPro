using System.Net;
using System.Text;
using System.Text.Json;
using CoilPro.Commands.Internal.Transport;
using Google.Protobuf;
using UniversalMessage;

namespace CoilPro.Commands.Tests.Transport;

public sealed class FleetSignedCommandTransportTests
{
    private const string Vin = "5YJ3E1EA7KF317000";

    private sealed class FixedTokenProvider : ITeslaTokenProvider
    {
        public ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default) => new("token-123");
    }

    private sealed class RecordedRequest
    {
        public required HttpMethod Method { get; init; }
        public required Uri Uri { get; init; }
        public required string? Authorization { get; init; }
        public required string Body { get; init; }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();

        public List<RecordedRequest> Requests { get; } = [];

        public StubHandler Enqueue(HttpStatusCode status, string body, Action<HttpResponseMessage>? configure = null)
        {
            _responses.Enqueue(() =>
            {
                var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                configure?.Invoke(response);
                return response;
            });
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest
            {
                Method = request.Method,
                Uri = request.RequestUri!,
                Authorization = request.Headers.Authorization?.ToString(),
                Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
            });
            return _responses.Dequeue()();
        }
    }

    private static RoutableMessage Message(params byte[] uuid) => new() { Uuid = ByteString.CopyFrom(uuid) };

    private static string ReplyJson(RoutableMessage reply) =>
        JsonSerializer.Serialize(new Dictionary<string, string> { ["response"] = Convert.ToBase64String(reply.ToByteArray()) });

    private static (FleetSignedCommandTransport Transport, StubHandler Handler) Create()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fleet-api.prd.eu.vn.cloud.tesla.com/") };
        return (new FleetSignedCommandTransport(http, new FixedTokenProvider(), TimeProvider.System, TimeSpan.Zero), handler);
    }

    [Fact]
    public async Task Posts_base64_routable_message_with_bearer_to_vin_path_and_parses_reply()
    {
        var (transport, handler) = Create();
        var sent = Message(1, 2, 3);
        var reply = Message(9, 8, 7);
        handler.Enqueue(HttpStatusCode.OK, ReplyJson(reply));

        var result = await transport.SendAsync(Vin, sent, CancellationToken.None);

        Assert.Equal(reply, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://fleet-api.prd.eu.vn.cloud.tesla.com/api/1/vehicles/{Vin}/signed_command", request.Uri.ToString());
        Assert.Equal("Bearer token-123", request.Authorization);
        using var doc = JsonDocument.Parse(request.Body);
        var encoded = doc.RootElement.GetProperty("routable_message").GetString();
        Assert.Equal(sent, RoutableMessage.Parser.ParseFrom(Convert.FromBase64String(encoded!)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData("5YJ3E1EA7KF3170001")]
    public async Task Rejects_vin_that_is_not_17_chars(string vin)
    {
        var (transport, handler) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => transport.SendAsync(vin, Message(1), CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Status_503_is_asleep()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, "");

        var ex = await Assert.ThrowsAsync<TeslaVehicleAsleepException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(Vin, ex.Vin);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Status_408_vehicle_is_offline_is_asleep_and_not_retried()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.RequestTimeout, "{\"error\":\"vehicle is offline or asleep\"}");

        await Assert.ThrowsAsync<TeslaVehicleAsleepException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Status_408_other_resends_same_bytes()
    {
        var (transport, handler) = Create();
        var reply = Message(5);
        handler.Enqueue(HttpStatusCode.RequestTimeout, "timeout")
            .Enqueue(HttpStatusCode.RequestTimeout, "timeout")
            .Enqueue(HttpStatusCode.OK, ReplyJson(reply));

        var result = await transport.SendAsync(Vin, Message(1), CancellationToken.None);

        Assert.Equal(reply, result);
        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, r => Assert.Equal(handler.Requests[0].Body, r.Body));
    }

    [Fact]
    public async Task Status_408_gives_up_after_three_attempts()
    {
        var (transport, handler) = Create();
        for (var i = 0; i < 3; i++)
        {
            handler.Enqueue(HttpStatusCode.RequestTimeout, "timeout");
        }

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(HttpStatusCode.RequestTimeout, ex.StatusCode);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Status_421_switches_to_named_tesla_host_and_resends_once()
    {
        var (transport, handler) = Create();
        var reply = Message(5);
        handler.Enqueue(HttpStatusCode.MisdirectedRequest, "{\"error\":\"use base URL: https://fleet-api.prd.na.vn.cloud.tesla.com\"}")
            .Enqueue(HttpStatusCode.OK, ReplyJson(reply));

        var result = await transport.SendAsync(Vin, Message(1), CancellationToken.None);

        Assert.Equal(reply, result);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal($"https://fleet-api.prd.na.vn.cloud.tesla.com/api/1/vehicles/{Vin}/signed_command", handler.Requests[1].Uri.ToString());
        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body);
    }

    [Fact]
    public async Task Status_421_with_foreign_host_is_rejected_without_resend()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.MisdirectedRequest, "use base URL: https://evil.example.com");

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(HttpStatusCode.MisdirectedRequest, ex.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Status_421_twice_is_not_followed_a_second_time()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.MisdirectedRequest, "use base URL: https://a.tesla.com")
            .Enqueue(HttpStatusCode.MisdirectedRequest, "use base URL: https://b.tesla.com");

        await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Status_422_means_protocol_unsupported()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.UnprocessableEntity, "");

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Contains("Vehicle Command Protocol", ex.Message);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Status_429_reports_retry_after_and_is_not_retried()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.TooManyRequests, "slow down", r => r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(42)));

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Contains("42", ex.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Other_failures_are_never_resent(HttpStatusCode status)
    {
        var (transport, handler) = Create();
        handler.Enqueue(status, "boom");

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Equal(status, ex.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Oversized_response_is_rejected()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.OK, new string('a', FleetSignedCommandTransport.MaxResponseBytes + 1));

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));

        Assert.Contains("maximum length", ex.Message);
    }

    [Fact]
    public async Task Undecodable_reply_is_a_command_exception()
    {
        var (transport, handler) = Create();
        handler.Enqueue(HttpStatusCode.OK, "{\"response\":\"!!!not base64!!!\"}");

        await Assert.ThrowsAsync<TeslaCommandException>(() => transport.SendAsync(Vin, Message(1), CancellationToken.None));
    }
}
