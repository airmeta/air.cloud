# UDP 隧道接入

UDP 隧道接入用于把入口环境中的一个 UDP 监听端口映射到 Agent 所在网络中的一个固定 UDP 服务。它适用于 DNS、自定义 UDP 协议、部分设备协议等数据报场景。

UDP 接入不经过 Ocelot `DelegatingHandler`。Ocelot 只处理 HTTP 请求，UDP 由 AirTunnel Server 角色直接绑定端口，并按客户端端点维护 UDP flow 表。

## 链路

```text
外部 UDP 客户端
  -> AirTunnel Server UDP Listener
  -> Server UDP Flow
  -> Agent gRPC 双向流
  -> Agent 本地 UDP 服务
```

## Agent 配置与 Server 派生

UDP 映射统一在 Agent 配置中维护。Server 侧不需要重复新增 listener；保存 Agent 配置后，Server 从 `Udp.LocalServices` 自动派生对外 listener。`Port` 是 Agent 本地目标端口，`ServerListenPort` 是 Server 对外端口，两者必须分别配置。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-agent",
    "Udp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceName": "Local DNS",
          "Host": "127.0.0.1",
          "Port": 53,
          "ServerListenHost": "0.0.0.0",
          "ServerListenPort": 1053
        }
      ]
    }
  }
}
```

## Agent 运行配置

Agent 侧负责声明真实本地 UDP 目标。外部客户端不能指定目标地址。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-agent",
    "ServerAddress": "https://air-tunnel.example.com",
    "Udp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceName": "Local DNS",
          "Host": "127.0.0.1",
          "Port": 53,
          "ServerListenHost": "0.0.0.0",
          "ServerListenPort": 1053
        }
      ]
    }
  }
}
```

## 运行行为

- Server 自动绑定 Agent 配置声明的 `ServerListenHost:ServerListenPort`。
- Server 按 `监听器 + 外部客户端端点 + ServiceKey` 维护 UDP flow。
- 每个 UDP datagram 会被封装为一个隧道帧发送给 Agent。
- Agent 根据 `Udp.LocalServices[].ServiceKey` 找到本地 `Host:Port`。
- Agent 本地 UDP 服务的响应会按同一个 flow 返回给原外部客户端端点。
- flow 超过 `FlowIdleTimeoutSeconds` 后会被清理。

## UDP 语义

AirTunnel 不改变 UDP 的基础语义：

- 不保证送达。
- 不保证顺序。
- 不做重传。
- 不做 TCP 模拟。
- 应用协议需要自己处理丢包、乱序和重复包。

## 注意事项

- UDP 监听端口需要在部署环境、防火墙、安全组或反向代理层放行。
- `MaxDatagramBytes` 应按业务协议和网络 MTU 设置，避免过大的 UDP 分片。
- `ServiceKey` 由 `udp://ServerListenHost:ServerListenPort` 的规范化字符串计算 MD5，不能手工填写。
- `MaxFlows` 应按单实例内存和预期客户端端点数量设置。
- `FlowIdleTimeoutSeconds` 过短会导致频繁重建 flow，过长会增加无效 flow 占用。
- Server 配置页面只读展示派生 listener，真实目标地址和对外映射都由 Agent 配置维护。

## 验证

1. 启动 AirTunnel Server，确认 UDP 监听端口已绑定。
2. 启动 Agent，确认 Server 诊断接口能看到 Agent 声明了对应 `ServiceKey`。
3. 在 Agent 所在环境直接访问本地 UDP 服务，确认目标可用。
4. 从入口环境向 Server 暴露的 UDP 端口发送数据报，确认响应来自 Agent 本地服务。
