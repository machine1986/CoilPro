using CoilPro.Commands.Internal.Commands;
using UniversalMessage;

namespace CoilPro.Commands.Tests.Orchestration;

public sealed class CoilProCommandsOrchestrationTests
{
    private const string Vin = "5YJ3E1EA7KF317000";
    private const string OtherVin = "5YJ3E1EA7KF317001";

    private static readonly VehicleCommand Command = new("test", Domain.Infotainment, [1, 2, 3]);
    private static readonly VehicleCommand VcsecCommand = new("test_vcsec", Domain.VehicleSecurity, [4]);

    private sealed class Harness
    {
        public EventLog Log { get; } = new();
        public FakeProtocol Protocol { get; }
        public FakeTransport Transport { get; }
        public ManualTimeProvider Clock { get; } = new();
        public FakeCoilClient CoilPro { get; } = new();
        public CoilProCommandsOptions Options { get; } = new()
        {
            RetryDelay = TimeSpan.FromSeconds(1),
            CommandTimeout = TimeSpan.FromSeconds(30),
            WakePollInterval = TimeSpan.FromSeconds(3),
            WakeTimeout = TimeSpan.FromSeconds(60),
        };

        public Func<Domain, ReadOnlyMemory<byte>, CommandResult>? Interpreter { get; set; }

        public Harness()
        {
            Protocol = new FakeProtocol(Log);
            Transport = new FakeTransport(Log);
        }

        public CoilProCommands Create() => new(Transport, Protocol, Options.AutoWake ? CoilPro : null, Options, Clock, Interpreter);

        public async Task<T> RunAsync<T>(Task<T> task)
        {
            await Clock.PumpUntilCompletedAsync(task);
            return await task;
        }
    }

    private static TeslaCommandException Fault(TeslaMessageFault fault) => new($"fault {fault}", Vin, fault);

    [Fact]
    public async Task Happy_path_handshakes_signs_sends_opens_and_interprets()
    {
        var h = new Harness { Interpreter = (_, payload) => payload.Length == 2 ? CommandResult.Succeeded : CommandResult.Refused("x") };
        h.Protocol.OnOpen = (_, _, _) => new byte[] { 7, 7 };

        var result = await h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["handshake", "send", "establish", "sign", "send", "open"], h.Log.Snapshot());
    }

    [Fact]
    public async Task Refusal_from_interpreter_is_returned_not_thrown()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Refused("already_set") };

        var result = await h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("already_set", result.Reason);
    }

    [Fact]
    public async Task Session_is_reused_across_commands_but_not_across_domains_or_vins()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        var commands = h.Create();

        await commands.ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await commands.ExecuteAsync(Vin, () => Command, CancellationToken.None);
        Assert.Equal(1, h.Log.Count("establish"));

        await commands.ExecuteAsync(Vin, () => VcsecCommand, CancellationToken.None);
        Assert.Equal(2, h.Log.Count("establish"));

        await commands.ExecuteAsync(OtherVin, () => Command, CancellationToken.None);
        Assert.Equal(3, h.Log.Count("establish"));
        Assert.Equal(4, h.Log.Count("sign"));
    }

    [Fact]
    public async Task Retryable_fault_adopts_session_info_from_the_fault_reply_then_resigns()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Protocol.OnOpen = (n, _, _) => n == 1 ? throw Fault(TeslaMessageFault.InvalidTokenOrCounter) : ReadOnlyMemory<byte>.Empty;
        var commands = h.Create();

        var result = await h.RunAsync(commands.ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(["handshake", "send", "establish", "sign", "send", "open", "update", "sign", "send", "open"], h.Log.Snapshot());
        var update = Assert.Single(h.Protocol.UpdateCalls);
        Assert.Same(h.Transport.Replies[1], update.Reply);
        Assert.Equal(1, h.Log.Count("handshake"));
    }

    [Fact]
    public async Task Session_is_dropped_and_rehandshaken_when_session_info_cannot_be_adopted()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Protocol.TryUpdateResult = false;
        h.Protocol.OnOpen = (n, _, _) => n == 1 ? throw Fault(TeslaMessageFault.IncorrectEpoch) : ReadOnlyMemory<byte>.Empty;

        var result = await h.RunAsync(h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(2, h.Log.Count("handshake"));
        Assert.Equal(2, h.Log.Count("establish"));
    }

    [Fact]
    public async Task Fault_from_interpreter_is_retried_without_touching_the_session()
    {
        var h = new Harness();
        var attempts = 0;
        h.Interpreter = (_, _) => ++attempts == 1 ? throw Fault(TeslaMessageFault.Busy) : CommandResult.Succeeded;

        var result = await h.RunAsync(h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(0, h.Log.Count("update"));
        Assert.Equal(1, h.Log.Count("establish"));
        Assert.Equal(2, h.Log.Count("sign"));
    }

    [Fact]
    public async Task Retryable_fault_during_handshake_retries_the_handshake()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        var establishes = 0;
        h.Protocol.OnEstablish = (req, _) => ++establishes == 1 ? throw Fault(TeslaMessageFault.Busy) : new Internal.Protocol.VehicleSession(req.ToDestination.Domain);

        var result = await h.RunAsync(h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(2, h.Log.Count("handshake"));
    }

    [Fact]
    public async Task Non_retryable_fault_propagates_without_retry()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Protocol.OnOpen = (_, _, _) => throw Fault(TeslaMessageFault.InsufficientPrivileges);

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.Equal(TeslaMessageFault.InsufficientPrivileges, ex.Fault);
        Assert.Equal(1, h.Log.Count("sign"));
    }

    [Fact]
    public async Task Key_not_paired_propagates()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Protocol.OnOpen = (_, _, _) => throw new TeslaKeyNotPairedException(Vin, TeslaMessageFault.UnknownKeyId);

        await Assert.ThrowsAsync<TeslaKeyNotPairedException>(() => h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.Equal(1, h.Log.Count("sign"));
    }

    [Fact]
    public async Task Key_not_paired_during_handshake_propagates()
    {
        var h = new Harness();
        h.Protocol.OnEstablish = (_, _) => throw new TeslaKeyNotPairedException(Vin);

        await Assert.ThrowsAsync<TeslaKeyNotPairedException>(() => h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.Equal(0, h.Log.Count("sign"));
    }

    [Fact]
    public async Task Asleep_without_auto_wake_throws()
    {
        var h = new Harness();
        h.Transport.OnSend = (_, _) => throw new TeslaVehicleAsleepException(Vin);

        await Assert.ThrowsAsync<TeslaVehicleAsleepException>(() => h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.Empty(h.CoilPro.Calls);
    }

    [Fact]
    public async Task Asleep_with_auto_wake_wakes_polls_until_online_and_retries_once()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Options.AutoWake = true;
        h.CoilPro.PollStates.Enqueue("asleep");
        h.CoilPro.PollStates.Enqueue("online");
        var first = true;
        h.Transport.OnSend = (n, _) =>
        {
            if (first)
            {
                first = false;
                throw new TeslaVehicleAsleepException(Vin);
            }

            return Task.FromResult(new RoutableMessage());
        };

        var result = await h.RunAsync(h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(["wake", "list", "list"], h.CoilPro.Calls);
    }

    [Fact]
    public async Task Still_asleep_after_wake_throws_instead_of_looping()
    {
        var h = new Harness();
        h.Options.AutoWake = true;
        h.CoilPro.WakeState = "online";
        h.Transport.OnSend = (_, _) => throw new TeslaVehicleAsleepException(Vin);

        await Assert.ThrowsAsync<TeslaVehicleAsleepException>(() => h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None));

        Assert.Equal(["wake"], h.CoilPro.Calls);
        Assert.Equal(2, h.Log.Count("send"));
    }

    [Fact]
    public async Task Vehicle_that_never_comes_online_times_out_as_asleep()
    {
        var h = new Harness();
        h.Options.AutoWake = true;
        h.CoilPro.PollStates.Enqueue("asleep");
        h.Transport.OnSend = (_, _) => throw new TeslaVehicleAsleepException(Vin);

        var task = h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await h.Clock.PumpUntilCompletedAsync(task);

        var ex = await Assert.ThrowsAsync<TeslaVehicleAsleepException>(() => task);
        Assert.Contains("did not come online", ex.Message);
    }

    [Fact]
    public async Task Command_times_out_with_last_fault_in_message()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        h.Protocol.OnOpen = (_, _, _) => throw Fault(TeslaMessageFault.Busy);

        var task = h.Create().ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await h.Clock.PumpUntilCompletedAsync(task);

        var ex = await Assert.ThrowsAsync<TeslaCommandException>(() => task);
        Assert.Contains("timed out after", ex.Message);
        Assert.Contains("waiting for vehicle", ex.Message);
        Assert.Equal(TeslaMessageFault.Busy, ex.Fault);
        Assert.True(h.Log.Count("sign") > 1);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_timeout()
    {
        var h = new Harness();
        using var cts = new CancellationTokenSource();
        h.Transport.OnSend = async (_, _) =>
        {
            await cts.CancelAsync();
            throw new OperationCanceledException(cts.Token);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Create().ExecuteAsync(Vin, () => Command, cts.Token));
    }

    [Fact]
    public async Task Commands_to_one_vin_do_not_interleave()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandSends = 0;

        // Handshake (call 1) passes; the first command send blocks until released.
        h.Transport.OnSend = async (n, _) =>
        {
            if (n > 1 && Interlocked.Increment(ref commandSends) == 1)
            {
                entered.SetResult();
                await gate.Task;
            }

            return new RoutableMessage();
        };
        var commands = h.Create();

        var first = commands.ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await entered.Task;
        var second = commands.ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await Task.Delay(50);

        Assert.Equal(1, h.Log.Count("sign"));
        Assert.False(second.IsCompleted);

        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(
            ["handshake", "send", "establish", "sign", "send", "open", "sign", "send", "open"],
            h.Log.Snapshot());
    }

    [Fact]
    public async Task Different_vins_are_not_serialized()
    {
        var h = new Harness { Interpreter = (_, _) => CommandResult.Succeeded };
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Transport.OnSend = async (n, message) =>
        {
            if (n == 1)
            {
                blocked.SetResult();
                await gate.Task;
            }

            return new RoutableMessage();
        };
        var commands = h.Create();

        var first = commands.ExecuteAsync(Vin, () => Command, CancellationToken.None);
        await blocked.Task;
        var second = await commands.ExecuteAsync(OtherVin, () => Command, CancellationToken.None);

        Assert.True(second.Success);
        Assert.False(first.IsCompleted);
        gate.SetResult();
        await first;
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    public async Task Invalid_vin_throws_before_any_io(string vin)
    {
        var h = new Harness();

        await Assert.ThrowsAsync<ArgumentException>(() => h.Create().ExecuteAsync(vin, () => Command, CancellationToken.None));

        Assert.Empty(h.Log.Snapshot());
    }

    [Fact]
    public async Task Argument_errors_from_the_payload_builder_propagate_before_any_io()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => h.Create().ExecuteAsync(Vin, () => throw new ArgumentOutOfRangeException("percent"), CancellationToken.None));

        Assert.Empty(h.Log.Snapshot());
    }

    [Fact]
    public void Auto_wake_without_client_is_rejected()
    {
        var h = new Harness();
        h.Options.AutoWake = true;

        Assert.Throws<ArgumentException>(() => new CoilProCommands(h.Transport, h.Protocol, null, h.Options, h.Clock));
    }
}
