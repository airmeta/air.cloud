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

## Server 配置

Server 侧负责绑定 UDP 端口。每个监听器固定绑定一个 `ServiceKey`。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Server",
    "Udp": {
      "Enabled": true,
      "Listeners": [
        {
          "Name": "dns-dev",
          "ServiceKey": "local-dns",
          "ListenHost": "0.0.0.0",
          "ListenPort": 1053,
          "MaxFlows": 1000,
          "FlowIdleTimeoutSeconds": 60,
          "MaxDatagramBytes": 1400
        }
      ]
    },
    "AgentAuthorizations": [
      {
        "AgentId": "branch-agent",
        "AllowedServiceKeys": [ "local-dns" ]
      }
    ]
  }
}
```

## Agent 配置

Agent 侧负责声明真实本地 UDP 目标。外部客户端不能指定目标地址。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Agent",
    "AgentId": "branch-agent",
    "ServerAddress": "https://air-tunnel.example.com",
    "Udp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceKey": "local-dns",
          "Host": "127.0.0.1",
          "Port": 53
        }
      ]
    }
  }
}
```

## 运行行为

- Server 绑定 `ListenHost:ListenPort`。
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
- `MaxFlows` 应按单实例内存和预期客户端端点数量设置。
- `FlowIdleTimeoutSeconds` 过短会导致频繁重建 flow，过长会增加无效 flow 占用。
- 真实目标地址只允许写在 Agent 本地配置中。

## 验证

1. 启动 AirTunnel Server，确认 UDP 监听端口已绑定。
2. 启动 Agent，确认 Server 诊断接口能看到 Agent 声明了对应 `ServiceKey`。
3. 在 Agent 所在环境直接访问本地 UDP 服务，确认目标可用。
4. 从入口环境向 Server 暴露的 UDP 端口发送数据报，确认响应来自 Agent 本地服务。
