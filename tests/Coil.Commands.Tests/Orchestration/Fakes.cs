using Coil.Commands.Internal.Protocol;
using Coil.Commands.Internal.Transport;
using Google.Protobuf;
using UniversalMessage;

namespace Coil.Commands.Tests.Orchestration;

internal sealed class EventLog
{
    private readonly List<string> _events = [];

    public void Add(string e)
    {
        lock (_events)
        {
            _events.Add(e);
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_events)
        {
            return [.. _events];
        }
    }

    public int Count(string e) => Snapshot().Count(x => x == e);
}

internal sealed class FakeProtocol(EventLog log) : IVehicleProtocol
{
    private int _opens;
    private int _signs;

    public Func<RoutableMessage, RoutableMessage, VehicleSession>? OnEstablish { get; set; }

    /// <summary>(open attempt number starting at 1, prepared, reply) → plaintext, or throws.</summary>
    public Func<int, PreparedCommand, RoutableMessage, ReadOnlyMemory<byte>>? OnOpen { get; set; }

    public bool TryUpdateResult { get; set; } = true;

    public List<(PreparedCommand Request, RoutableMessage Reply)> UpdateCalls { get; } = [];

    public RoutableMessage CreateSessionInfoRequest(Domain domain)
    {
        log.Add("handshake");
        return new RoutableMessage { ToDestination = new Destination { Domain = domain } };
    }

    public VehicleSession EstablishSession(string vin, RoutableMessage request, RoutableMessage reply, DateTimeOffset requestSentAt)
    {
        log.Add("establish");
        return OnEstablish?.Invoke(request, reply) ?? new VehicleSession(request.ToDestination.Domain);
    }

    public bool TryUpdateSession(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply)
    {
        log.Add("update");
        UpdateCalls.Add((request, reply));
        return TryUpdateResult;
    }

    public PreparedCommand Sign(VehicleSession session, string vin, ReadOnlyMemory<byte> payload)
    {
        var n = Interlocked.Increment(ref _signs);
        log.Add("sign");
        return new PreparedCommand(new RoutableMessage { Uuid = ByteString.CopyFrom(BitConverter.GetBytes(n)) });
    }

    public ReadOnlyMemory<byte> OpenReply(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply)
    {
        var n = Interlocked.Increment(ref _opens);
        log.Add("open");
        return OnOpen?.Invoke(n, request, reply) ?? ReadOnlyMemory<byte>.Empty;
    }
}

internal sealed class FakeTransport(EventLog log) : ISignedCommandTransport
{
    private int _calls;

    /// <summary>(call number starting at 1, message) → reply, or throws.</summary>
    public Func<int, RoutableMessage, Task<RoutableMessage>>? OnSend { get; set; }

    public List<RoutableMessage> Replies { get; } = [];

    public async Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct)
    {
        var n = Interlocked.Increment(ref _calls);
        log.Add("send");
        var reply = OnSend is null ? new RoutableMessage { Uuid = ByteString.CopyFrom(BitConverter.GetBytes(1000 + n)) } : await OnSend(n, message);
        lock (Replies)
        {
            Replies.Add(reply);
        }

        return reply;
    }
}

internal sealed class FakeCoilClient : ICoilClient
{
    public List<string> Calls { get; } = [];

    /// <summary>States returned by successive GetVehiclesAsync calls (last one repeats).</summary>
    public Queue<string> PollStates { get; } = new();

    public string WakeState { get; set; } = "asleep";

    public RateLimitInfo? LastRateLimit => null;

    public Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default)
    {
        Calls.Add("list");
        var state = PollStates.Count > 1 ? PollStates.Dequeue() : PollStates.Peek();
        return Task.FromResult<IReadOnlyList<Vehicle>>([new Vehicle { Vin = "5YJ3E1EA7KF317000", State = state }]);
    }

    public Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default)
    {
        Calls.Add("wake");
        return Task.FromResult(new Vehicle { Vin = vehicleTag, State = WakeState });
    }

    public Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ChargingHistoryPage> GetChargingHistoryAsync(CancellationToken ct = default) => throw new NotSupportedException();
}
