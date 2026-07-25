using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道协议帧测试集合。</para>
/// <para>en-us:Test suite for reverse tunnel protocol frames.</para>
/// </summary>
public class ReverseTunnelProtocolTests
{
    /// <summary>
    /// <para>zh-cn:验证连接控制帧仅接受流标识 0，避免连接级帧被误关联到业务请求。</para>
    /// <para>en-us:Verifies that connection-control frames accept only stream identifier 0, preventing connection-level frames from being associated with business requests.</para>
    /// </summary>
    [Fact]
    public void Validate_should_accept_control_frame_with_zero_stream_identifier()
    {
        var frame = new ReverseTunnelFrame
        {
            SessionId = "agent-a-session",
            StreamId = 0,
            Kind = ReverseTunnelFrameKind.Ping
        };

        frame.Validate();

        Assert.True(frame.IsControlFrame());
    }

    /// <summary>
    /// <para>zh-cn:验证数据帧缺少正数流标识时会被拒绝，确保并发请求流无法与控制帧混淆。</para>
    /// <para>en-us:Verifies that a data frame without a positive stream identifier is rejected, ensuring concurrent request streams cannot be confused with control frames.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_data_frame_with_zero_stream_identifier()
    {
        var frame = new ReverseTunnelFrame
        {
            SessionId = "agent-a-session",
            StreamId = 0,
            Kind = ReverseTunnelFrameKind.OpenRequest
        };

        var exception = Assert.Throws<InvalidOperationException>(frame.Validate);

        Assert.Contains("positive stream identifier", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证旧协议版本不能进入当前实现，避免未协商的帧格式被错误解析。</para>
    /// <para>en-us:Verifies that an older protocol version cannot enter the current implementation, avoiding incorrect parsing of unnegotiated frame formats.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_unsupported_protocol_version()
    {
        var frame = new ReverseTunnelFrame
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion + 1,
            SessionId = "agent-a-session",
            Kind = ReverseTunnelFrameKind.Hello
        };

        var exception = Assert.Throws<InvalidOperationException>(frame.Validate);

        Assert.Contains("Unsupported reverse tunnel protocol version", exception.Message, StringComparison.Ordinal);
    }
}
