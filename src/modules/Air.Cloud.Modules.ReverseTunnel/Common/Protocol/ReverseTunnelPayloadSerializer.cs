using System.Text.Json;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道帧负载序列化器。当前版本使用 UTF-8 JSON 固化 HTTP 元数据负载，二进制请求体和响应体仍直接放入 body 帧。</para>
/// <para>en-us:Serializer for reverse tunnel frame payloads. The current version uses UTF-8 JSON for HTTP metadata payloads, while binary request and response bodies remain directly carried by body frames.</para>
/// </summary>
public static class ReverseTunnelPayloadSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <para>zh-cn:把 HTTP 请求元数据序列化为 OpenRequest 帧负载。</para>
    /// <para>en-us:Serializes HTTP request metadata into an OpenRequest frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:HTTP 请求元数据。</para>
    /// <para>en-us:HTTP request metadata.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeRequest(ReverseTunnelHttpRequestPayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 OpenRequest 帧负载反序列化 HTTP 请求元数据。</para>
    /// <para>en-us:Deserializes HTTP request metadata from an OpenRequest frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:HTTP 请求元数据。</para>
    /// <para>en-us:HTTP request metadata.</para>
    /// </returns>
    public static ReverseTunnelHttpRequestPayload DeserializeRequest(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelHttpRequestPayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel request payload is empty.");
    }

    /// <summary>
    /// <para>zh-cn:把 HTTP 响应元数据序列化为 ResponseHeaders 帧负载。</para>
    /// <para>en-us:Serializes HTTP response metadata into a ResponseHeaders frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:HTTP 响应元数据。</para>
    /// <para>en-us:HTTP response metadata.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeResponse(ReverseTunnelHttpResponsePayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 ResponseHeaders 帧负载反序列化 HTTP 响应元数据。</para>
    /// <para>en-us:Deserializes HTTP response metadata from a ResponseHeaders frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:HTTP 响应元数据。</para>
    /// <para>en-us:HTTP response metadata.</para>
    /// </returns>
    public static ReverseTunnelHttpResponsePayload DeserializeResponse(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelHttpResponsePayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel response payload is empty.");
    }

    /// <summary>
    /// <para>zh-cn:把错误信息序列化为 Error 帧负载。</para>
    /// <para>en-us:Serializes error information into an Error frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:错误信息。</para>
    /// <para>en-us:Error information.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeError(ReverseTunnelErrorPayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 Error 帧负载反序列化错误信息。</para>
    /// <para>en-us:Deserializes error information from an Error frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:错误信息。</para>
    /// <para>en-us:Error information.</para>
    /// </returns>
    public static ReverseTunnelErrorPayload DeserializeError(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelErrorPayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel error payload is empty.");
    }

    /// <summary>
    /// <para>zh-cn:把 TCP 打开信息序列化为 TcpOpen 帧负载。</para>
    /// <para>en-us:Serializes TCP open information into a TcpOpen frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:TCP 打开信息。</para>
    /// <para>en-us:TCP open information.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeTcpOpen(ReverseTunnelTcpOpenPayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 TcpOpen 帧负载反序列化 TCP 打开信息。</para>
    /// <para>en-us:Deserializes TCP open information from a TcpOpen frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:TCP 打开信息。</para>
    /// <para>en-us:TCP open information.</para>
    /// </returns>
    public static ReverseTunnelTcpOpenPayload DeserializeTcpOpen(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelTcpOpenPayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel TCP open payload is empty.");
    }

    /// <summary>
    /// <para>zh-cn:把 TCP 关闭信息序列化为 TcpClose 帧负载。</para>
    /// <para>en-us:Serializes TCP close information into a TcpClose frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:TCP 关闭信息。</para>
    /// <para>en-us:TCP close information.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeTcpClose(ReverseTunnelTcpClosePayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 TcpClose 帧负载反序列化 TCP 关闭信息。</para>
    /// <para>en-us:Deserializes TCP close information from a TcpClose frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:TCP 关闭信息。</para>
    /// <para>en-us:TCP close information.</para>
    /// </returns>
    public static ReverseTunnelTcpClosePayload DeserializeTcpClose(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelTcpClosePayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel TCP close payload is empty.");
    }

    /// <summary>
    /// <para>zh-cn:把 UDP 数据报序列化为 UdpDatagram 帧负载。</para>
    /// <para>en-us:Serializes a UDP datagram into a UdpDatagram frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UDP 数据报信息。</para>
    /// <para>en-us:UDP datagram information.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UTF-8 JSON 字节。</para>
    /// <para>en-us:UTF-8 JSON bytes.</para>
    /// </returns>
    public static byte[] SerializeUdpDatagram(ReverseTunnelUdpDatagramPayload payload) => JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:从 UdpDatagram 帧负载反序列化 UDP 数据报信息。</para>
    /// <para>en-us:Deserializes UDP datagram information from a UdpDatagram frame payload.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:UTF-8 JSON 负载字节。</para>
    /// <para>en-us:UTF-8 JSON payload bytes.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:UDP 数据报信息。</para>
    /// <para>en-us:UDP datagram information.</para>
    /// </returns>
    public static ReverseTunnelUdpDatagramPayload DeserializeUdpDatagram(byte[] payload)
    {
        return JsonSerializer.Deserialize<ReverseTunnelUdpDatagramPayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel UDP datagram payload is empty.");
    }
}
