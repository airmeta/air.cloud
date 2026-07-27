using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;
using Microsoft.Extensions.Options;
using Google.Protobuf;
using System.Diagnostics.Metrics;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public class ReverseTunnelSessionRegistryTests
{
    [Fact]
    public void Register_should_replace_previous_session_for_same_agent()
    {
        var registry = CreateRegistry(30);
        registry.Register(new ReverseTunnelSession("session-one", "agent-a", ["order-service"], DateTimeOffset.UtcNow));
        registry.Register(new ReverseTunnelSession("session-two", "agent-a", ["order-service"], DateTimeOffset.UtcNow));

        Assert.False(registry.TryGet("session-one", out _));
        Assert.True(registry.TryGet("session-two", out var session));
        Assert.Equal("agent-a", session!.AgentId);
    }

    [Fact]
    public void RemoveExpired_should_remove_idle_sessions_only()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(10);
        registry.Register(new ReverseTunnelSession("expired", "agent-a", ["order-service"], now.AddSeconds(-11)));
        registry.Register(new ReverseTunnelSession("active", "agent-b", ["catalog-service"], now.AddSeconds(-9)));

        var removed = registry.RemoveExpired(now);

        Assert.Equal(1, removed);
        Assert.False(registry.TryGet("expired", out _));
        Assert.True(registry.TryGet("active", out _));
    }

    [Fact]
    public void TryGetForServiceKey_should_return_active_session_declaring_service()
    {
        var registry = CreateRegistry(30);
        registry.Register(new ReverseTunnelSession("session-one", "agent-a", ["order-service"], DateTimeOffset.UtcNow));

        var found = registry.TryGetForServiceKey("ORDER-SERVICE", out var session);

        Assert.True(found);
        Assert.Equal("session-one", session!.SessionId);
    }

    [Fact]
    public void TryGetForServiceKey_should_keep_newest_active_session_as_default_strategy()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(30);
        registry.Register(new ReverseTunnelSession("session-old", "agent-old", ["order-service"], now.AddSeconds(-10)));
        registry.Register(new ReverseTunnelSession("session-new", "agent-new", ["order-service"], now));

        var found = registry.TryGetForServiceKey("order-service", out var session);

        Assert.True(found);
        Assert.Equal("session-new", session!.SessionId);
    }

    [Fact]
    public void TryGetForServiceKey_should_select_oldest_active_session_when_configured()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(30, ReverseTunnelSessionSelectionStrategy.OldestActive);
        registry.Register(new ReverseTunnelSession("session-old", "agent-old", ["order-service"], now.AddSeconds(-10)));
        registry.Register(new ReverseTunnelSession("session-new", "agent-new", ["order-service"], now));

        var found = registry.TryGetForServiceKey("order-service", out var session);

        Assert.True(found);
        Assert.Equal("session-old", session!.SessionId);
    }

    [Fact]
    public void TryGetForServiceKey_should_round_robin_sessions_when_configured()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(30, ReverseTunnelSessionSelectionStrategy.RoundRobin);
        registry.Register(new ReverseTunnelSession("session-a", "agent-a", ["order-service"], now));
        registry.Register(new ReverseTunnelSession("session-b", "agent-b", ["order-service"], now));
        registry.Register(new ReverseTunnelSession("session-c", "agent-c", ["order-service"], now));

        var selected = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            Assert.True(registry.TryGetForServiceKey("order-service", out var session));
            selected.Add(session!.SessionId);
        }

        Assert.Equal(["session-a", "session-b", "session-c", "session-a", "session-b", "session-c"], selected);
    }

    [Fact]
    public async Task InvokeAsync_should_reject_request_when_concurrent_stream_limit_is_reached()
    {
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 1024,
            maxRequestBodyBytes: 1024);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var firstInvocation = session.InvokeAsync(CreateRequest(), Array.Empty<byte>(), TimeSpan.FromSeconds(5), timeout.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.OpenRequest, timeout.Token);

        var secondInvocation = await session.InvokeAsync(CreateRequest(), Array.Empty<byte>(), TimeSpan.FromSeconds(1), timeout.Token);

        Assert.False(secondInvocation.Succeeded);
        Assert.Equal("too_many_concurrent_streams", secondInvocation.ErrorCode);

        CompleteInvocation(session, streamId: 1);
        Assert.True((await firstInvocation).Succeeded);
    }

    [Fact]
    public async Task InvokeAsync_should_split_request_body_by_configured_frame_payload_limit()
    {
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 1024);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var body = new byte[2500];

        var invocation = session.InvokeAsync(CreateRequest(), body, TimeSpan.FromSeconds(5), timeout.Token);
        var frames = new List<TunnelFrameMessage>();
        while (frames.All(frame => frame.Kind != TunnelFrameKindMessage.Complete))
        {
            frames.Add(await session.OutboundFrames.ReadAsync(timeout.Token));
        }

        var bodyFrameLengths = frames
            .Where(frame => frame.Kind == TunnelFrameKindMessage.RequestBody)
            .Select(frame => frame.Payload.Length)
            .ToArray();

        Assert.Equal([1024, 1024, 452], bodyFrameLengths);

        CompleteInvocation(session, streamId: 1);
        Assert.True((await invocation).Succeeded);
    }

    [Fact]
    public async Task HandleInboundFrame_should_fail_invocation_when_response_body_exceeds_limit()
    {
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 1024);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var invocation = session.InvokeAsync(CreateRequest(), Array.Empty<byte>(), TimeSpan.FromSeconds(5), timeout.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Complete, timeout.Token);

        session.HandleInboundFrame(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.ResponseHeaders,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(new ReverseTunnelHttpResponsePayload
            {
                StatusCode = 200
            }))
        });
        session.HandleInboundFrame(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.ResponseBody,
            Payload = ByteString.CopyFrom(new byte[2048])
        });

        var result = await invocation;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in result.BodyReader!.ReadAllAsync(timeout.Token))
            {
            }
        });

        Assert.True(result.Succeeded);
        Assert.Contains("response body exceeds", exception.Message, StringComparison.OrdinalIgnoreCase);

        var cancelFrame = await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Cancel, timeout.Token);
        Assert.Equal(1, cancelFrame.StreamId);
    }

    [Fact]
    public async Task InvokeAsync_should_return_after_response_headers_and_stream_body_frames()
    {
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 4096);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var invocation = session.InvokeAsync(CreateRequest(), Array.Empty<byte>(), TimeSpan.FromSeconds(5), timeout.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Complete, timeout.Token);

        await session.HandleInboundFrameAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.ResponseHeaders,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(new ReverseTunnelHttpResponsePayload
            {
                StatusCode = 200
            }))
        }, timeout.Token);
        var result = await invocation;

        await session.HandleInboundFrameAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.ResponseBody,
            Payload = ByteString.CopyFrom("chunk-one"u8.ToArray())
        }, timeout.Token);
        var streamedBody = await result.BodyReader!.ReadAsync(timeout.Token);
        await session.HandleInboundFrameAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.Complete
        }, timeout.Token);

        Assert.True(result.Succeeded);
        Assert.Equal("chunk-one", System.Text.Encoding.UTF8.GetString(streamedBody));
    }

    [Fact]
    public async Task InvokeAsync_should_send_cancel_frame_when_timed_out()
    {
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 4096);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var invocation = session.InvokeAsync(CreateRequest(), Array.Empty<byte>(), TimeSpan.FromMilliseconds(50), timeout.Token);
        var cancelFrame = await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Cancel, timeout.Token);
        var result = await invocation;

        Assert.Equal(1, cancelFrame.StreamId);
        Assert.False(result.Succeeded);
        Assert.Equal("timeout", result.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_should_emit_request_metrics_for_successful_stream()
    {
        var measurementNames = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics.ReverseTunnelMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) => measurementNames.Add(instrument.Name));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) => measurementNames.Add(instrument.Name));
        listener.Start();

        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 4096);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var invocation = session.InvokeAsync(CreateRequest(), "metrics-body"u8.ToArray(), TimeSpan.FromSeconds(5), timeout.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Complete, timeout.Token);
        CompleteInvocation(session, streamId: 1);
        var result = await invocation;

        Assert.True(result.Succeeded);
        Assert.Contains("aircloud_reverse_tunnel_requests_total", measurementNames);
        Assert.Contains("aircloud_reverse_tunnel_request_duration_ms", measurementNames);
        Assert.Contains("aircloud_reverse_tunnel_request_bytes_total", measurementNames);
    }

    private static async Task<TunnelFrameMessage> WaitForOutboundFrameAsync(ReverseTunnelSession session, TunnelFrameKindMessage expectedKind, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await session.OutboundFrames.ReadAsync(cancellationToken);
            if (frame.Kind == expectedKind)
            {
                return frame;
            }
        }
    }

    private static ReverseTunnelHttpRequestPayload CreateRequest()
    {
        return new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "/api/orders"
        };
    }

    private static void CompleteInvocation(ReverseTunnelSession session, long streamId)
    {
        session.HandleInboundFrame(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = streamId,
            Kind = TunnelFrameKindMessage.ResponseHeaders,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(new ReverseTunnelHttpResponsePayload
            {
                StatusCode = 204
            }))
        });
        session.HandleInboundFrame(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = streamId,
            Kind = TunnelFrameKindMessage.Complete
        });
    }

    private static InMemoryReverseTunnelSessionRegistry CreateRegistry(int timeoutSeconds, ReverseTunnelSessionSelectionStrategy selectionStrategy = ReverseTunnelSessionSelectionStrategy.NewestActive)
    {
        return new InMemoryReverseTunnelSessionRegistry(Options.Create(new ReverseTunnelOptions
        {
            SessionIdleTimeoutSeconds = timeoutSeconds,
            SessionSelectionStrategy = selectionStrategy
        }));
    }
}
