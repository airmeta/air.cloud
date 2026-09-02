# TCP 隧道接入

TCP 隧道接入用于把入口环境中的一个 TCP 监听端口映射到 Agent 所在网络中的一个固定 TCP 服务。它适用于数据库、Redis、SSH、自定义 TCP 协议等非 HTTP 字节流场景。

TCP 接入不经过 Ocelot `DelegatingHandler`。Ocelot 只处理 HTTP 请求，TCP 由 AirTunnel Server 角色直接监听端口，再通过已建立的 Agent gRPC 双向流转发到 Agent 本地目标。

## 链路

```text
外部 TCP 客户端
  -> AirTunnel Server TCP Listener
  -> Agent gRPC 双向流
  -> Agent 本地 TCP 服务
```

## Agent 配置与 Server 派生

TCP 映射统一在 Agent 配置中维护。Server 侧不需要重复新增 listener；保存 Agent 配置后，Server 从 `Tcp.LocalServices` 自动派生对外 listener。`Port` 是 Agent 本地目标端口，`ServerListenPort` 是 Server 对外端口，两者必须分别配置。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-agent",
    "Tcp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceName": "Local MySQL",
          "Host": "127.0.0.1",
          "Port": 3306,
          "ServerListenHost": "0.0.0.0",
          "ServerListenPort": 13306
        }
      ]
    }
  }
}
```

## Agent 运行配置

Agent 侧负责声明真实本地 TCP 目标。外部客户端不能指定目标地址。

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-agent",
    "ServerAddress": "https://air-tunnel.example.com",
    "Tcp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceName": "Local MySQL",
          "Host": "127.0.0.1",
          "Port": 3306,
          "ServerListenHost": "0.0.0.0",
          "ServerListenPort": 13306
        }
      ]
    }
  }
}
```

## 运行行为

- Server 自动监听 Agent 配置声明的 `ServerListenHost:ServerListenPort`。
- 外部 TCP 客户端接入后，Server 根据监听器上的 `ServiceKey` 选择 Agent 会话。
- Server 向 Agent 发送 `TcpOpen` 帧。
- Agent 根据 `Tcp.LocalServices[].ServiceKey` 找到本地 `Host:Port`。
- 两端按 `MaxFramePayloadBytes` 拆分 TCP 字节流数据帧。
- 任意一端关闭连接时，另一端会收到关闭帧并释放 socket。

## 注意事项

- TCP 监听端口需要在部署环境、防火墙、安全组或反向代理层放行。
- `ServiceKey` 由 `tcp://ServerListenHost:ServerListenPort` 的规范化字符串计算 MD5，不能手工填写。
- Server 配置页面只读展示派生 listener，真实目标地址和对外映射都由 Agent 配置维护。
- 不要把 TCP 入口配置成可由外部请求动态指定目标地址。
- `MaxConnections` 应按单实例承载能力配置；多实例扩容属于部署层面。
- 长连接协议应设置合理的 `IdleTimeoutSeconds`，避免断开的客户端长期占用流表和 socket。

## 验证

1. 启动 AirTunnel Server，确认 TCP 监听端口已绑定。
2. 启动 Agent，确认 Server 诊断接口能看到 Agent 声明了对应 `ServiceKey`。
3. 在 Agent 所在环境直接访问本地目标 TCP 服务，确认目标可用。
4. 从入口环境访问 Server 暴露的 TCP 端口，确认请求到达 Agent 本地服务。
