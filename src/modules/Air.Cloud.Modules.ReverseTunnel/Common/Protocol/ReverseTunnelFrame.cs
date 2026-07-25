namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道中的统一帧模型。Payload 的编码由 FrameKind 决定；首版通过该稳定模型冻结帧边界，后续再映射到 Protobuf 消息。</para>
/// <para>en-us:Unified frame model for the reverse tunnel. FrameKind determines Payload encoding; the first version freezes the frame boundary with this stable model before mapping it to Protobuf messages.</para>
/// </summary>
public sealed class ReverseTunnelFrame
{
    /// <summary>
    /// <para>zh-cn:发送方采用的协议版本，必须等于当前受支持版本。</para>
    /// <para>en-us:Protocol version used by the sender. It must equal the currently supported version.</para>
    /// </summary>
    public int ProtocolVersion { get; set; } = ReverseTunnelProtocol.CurrentVersion;

    /// <summary>
    /// <para>zh-cn:连接会话标识。该值由 Agent 在 Hello 前生成，用于关联日志和后续帧。</para>
    /// <para>en-us:Connection session identifier. The Agent generates it before Hello to correlate logs and subsequent frames.</para>
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:逻辑请求流标识。连接控制帧必须为 0，数据帧必须为正数。</para>
    /// <para>en-us:Logical request-stream identifier. Connection-control frames must be 0 and data frames must be positive.</para>
    /// </summary>
    public long StreamId { get; set; }

    /// <summary>
    /// <para>zh-cn:帧类型。</para>
    /// <para>en-us:Frame kind.</para>
    /// </summary>
    public ReverseTunnelFrameKind Kind { get; set; }

    /// <summary>
    /// <para>zh-cn:由帧类型解释的二进制内容。空载荷使用空数组而不是 null。</para>
    /// <para>en-us:Binary content interpreted by the frame kind. Empty payloads use an empty array rather than null.</para>
    /// </summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// <para>zh-cn:验证帧是否满足首版协议边界。该方法仅检查传输前可判定的结构，不解析具体帧载荷。</para>
    /// <para>en-us:Validates that a frame satisfies the first-version protocol boundary. It only checks structure determinable before transport and does not parse the frame-specific payload.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:协议版本、会话标识或流标识不符合当前帧类型时抛出。</para>
    /// <para>en-us:Thrown when the protocol version, session identifier, or stream identifier does not match the current frame kind.</para>
    /// </exception>
    public void Validate()
    {
        if (ProtocolVersion != ReverseTunnelProtocol.CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported reverse tunnel protocol version: {ProtocolVersion}.");
        }

        if (string.IsNullOrWhiteSpace(SessionId))
        {
            throw new InvalidOperationException("Reverse tunnel frame session identifier is required.");
        }

        if (IsControlFrame() && StreamId != 0)
        {
            throw new InvalidOperationException($"Control frame {Kind} must use stream identifier 0.");
        }

        if (!IsControlFrame() && StreamId <= 0)
        {
            throw new InvalidOperationException($"Data frame {Kind} must use a positive stream identifier.");
        }

        Payload ??= Array.Empty<byte>();
    }

    /// <summary>
    /// <para>zh-cn:判断当前帧是否属于连接控制帧。</para>
    /// <para>en-us:Determines whether the current frame is a connection-control frame.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:属于连接控制帧时返回 true。</para>
    /// <para>en-us:True when the frame is a connection-control frame.</para>
    /// </returns>
    public bool IsControlFrame()
    {
        return Kind is ReverseTunnelFrameKind.Hello
            or ReverseTunnelFrameKind.Welcome
            or ReverseTunnelFrameKind.Ping
            or ReverseTunnelFrameKind.Pong;
    }
}
