# 验证与排查

AirTunnel 验证应从四个边界依次确认：Server endpoint、Agent 会话、Gateway 路由、后端 HTTP 服务。

## Server endpoint

确认 Server 进程已启动，并且配置满足：

- `ReverseTunnelSettings:Enabled=true`。
- `ReverseTunnelSettings:Roles` 包含 `Server`。
- `UseReverseTunnelServer()` 或 `MapReverseTunnelServer()` 已被调用。
- CA PFX 文件可读，且密码环境变量存在。
- 如果启用 Enrollment，凭据目录存在且 Server 进程可读写。

常见错误：

| 现象 | 检查项 |
| --- | --- |
| Server 启动时报配置校验失败 | CA 文件路径、CA 密码环境变量、Enrollment 目录 |
| Agent 连接返回 `Unauthenticated` | Agent 客户端证书是否由 Server 信任 CA 签发，证书 AgentId 是否匹配 |
| Agent 连接返回 `PermissionDenied` | `AgentAuthorizations` 是否允许该 Agent 声明对应 `ServiceKey` |

## Agent 会话

确认 Agent 进程已启动，并且可以主动访问 `ServerAddress`。

Agent 侧配置至少应包含：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Agent",
    "AgentId": "agent-order",
    "ServerAddress": "https://air-tunnel.example.com",
    "LocalServices": [
      {
        "ServiceKey": "order-service",
        "LocalAddress": "http://order-service.internal:8080"
      }
    ]
  }
}
```

检查项：

- Agent 日志中应能看到连接 Server 的记录。
- 证书缺失时，Enrollment 应成功生成本地证书和私钥文件。
- 证书进入续签窗口时，续签成功后应原子替换本地证书文件。
- Agent 声明的每个 `ServiceKey` 都应在 Server 侧授权范围内。

## Gateway 路由

确认 Ocelot 路由已显式声明 `ReverseTunnelDelegatingHandler`：

```json
{
  "UpstreamPathTemplate": "/orders/{everything}",
  "DownstreamPathTemplate": "/{everything}",
  "DownstreamHostAndPorts": [
    { "Host": "tunnel-order-service", "Port": 80 }
  ],
  "DelegatingHandlers": [ "ReverseTunnelDelegatingHandler" ]
}
```

确认 Gateway 侧存在匹配虚拟主机的 AirTunnel 路由：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Gateway",
    "Routes": [
      {
        "VirtualHost": "tunnel-order-service",
        "ServiceKey": "order-service",
        "TimeoutSeconds": 30
      }
    ]
  }
}
```

如果请求没有进入 AirTunnel，优先检查：

- Ocelot 路由是否声明了 `ReverseTunnelDelegatingHandler`。
- `DownstreamHostAndPorts[].Host` 是否等于 `Routes[].VirtualHost`。
- Gateway 进程角色是否包含 `Gateway`。
- Agent 是否已经连接，并声明了相同的 `ServiceKey`。

## 后端 HTTP 服务

在 Agent 所在环境中直接访问 `LocalServices.LocalAddress`，确认后端服务可用：

```bash
curl -i http://order-service.internal:8080/health
```

再从入口 Gateway 访问对应上游路径：

```bash
curl -i https://gateway.example.com/orders/health
```

两次调用应到达同一个后端服务。不要把响应中的密钥、票据、Token 或其它敏感材料复制到日志、文档或聊天记录中。

## 状态码排查

| 状态码 | 含义 | 排查方向 |
| --- | --- | --- |
| `503 ServiceUnavailable` | Gateway 当前没有可用 Agent 会话，或会话已关闭 | Agent 是否连接，`ServiceKey` 是否一致，Server 是否摘除了空闲会话 |
| `504 GatewayTimeout` | Gateway 等待 Agent 响应超时 | `Routes[].TimeoutSeconds`、后端服务响应时间、Agent 到后端服务网络 |
| `499` | 上游客户端取消或响应体提前释放 | 客户端连接、入口代理超时、响应体是否被调用方提前关闭 |
| `413 RequestEntityTooLarge` | 请求体超过 `MaxRequestBodyBytes` | 上游请求大小、Gateway 配置 |
| `429 TooManyRequests` | 单 Agent 会话并发流达到 `MaxConcurrentStreams` | 并发量、Agent 数量、并发限制配置 |
| `502 BadGateway` | Agent 返回错误或响应处理失败 | Agent 日志、本地 HTTP 服务异常、响应体是否超过 `MaxResponseBodyBytes` |

## 指标和链路检查

AirTunnel 指标通过 `System.Diagnostics.Metrics` 暴露，Meter 名称为 `Air.Cloud.Modules.ReverseTunnel`。接入 OpenTelemetry 后，应至少确认以下指标可以被采集：

```text
aircloud_reverse_tunnel_requests_total
aircloud_reverse_tunnel_request_failures_total
aircloud_reverse_tunnel_request_duration_ms
aircloud_reverse_tunnel_request_bytes_total
aircloud_reverse_tunnel_response_bytes_total
aircloud_reverse_tunnel_active_streams
aircloud_reverse_tunnel_backpressure_waits_total
aircloud_reverse_tunnel_cancellations_total
aircloud_reverse_tunnel_timeouts_total
```

排查建议：

- 请求进入穿透链路但没有业务响应时，先看 `aircloud_reverse_tunnel_requests_total` 是否增加。
- 出现 `504 GatewayTimeout` 时，看 `aircloud_reverse_tunnel_timeouts_total` 是否同步增加。
- 客户端断开或上游取消时，看 `aircloud_reverse_tunnel_cancellations_total` 是否增加。
- 大 body 或慢下游场景下，看 `aircloud_reverse_tunnel_backpressure_waits_total` 是否增加。
- `aircloud_reverse_tunnel_active_streams` 长时间不下降时，检查 Agent 是否持续返回响应体或是否存在未完成的客户端连接。

AirTunnel 链路追踪通过 `System.Diagnostics.ActivitySource` 暴露，ActivitySource 名称同样为 `Air.Cloud.Modules.ReverseTunnel`。接入 OpenTelemetry tracing 后，应能看到 `airtunnel.gateway.invoke` span。

链路检查建议：

- span 上应包含 `reverse_tunnel.service_key`、`reverse_tunnel.virtual_host`、`reverse_tunnel.agent_id`、`reverse_tunnel.session_id` 和 `http.response.status_code`。
- 失败请求应包含 `error.type` 和 `error.message`，并标记为 error 状态。
- 后端 HTTP 服务应能收到 `traceparent`；如果入口请求已经携带 `traceparent`，AirTunnel 不覆盖该值。
- 如果后端服务没有收到 `traceparent`，检查入口 Gateway 是否接入 tracing、请求是否命中 `ReverseTunnelDelegatingHandler`、以及 Agent 本地 HTTP 分发是否过滤了自定义请求头。

## 部署验收

AirTunnel 部署后，应至少完成以下验收：

- Server 可以加载 CA 文件和签发配置。
- Agent 可以完成首次 Enrollment 或使用已有证书建立 mTLS 连接。
- Server 侧可以看到 Agent 会话和对应 `ServiceKey`。
- Gateway 路由命中 `ReverseTunnelDelegatingHandler` 后，请求进入 AirTunnel。
- Agent 所在环境可以访问 `LocalServices.LocalAddress` 指向的后端 HTTP 服务。
- 从入口 Gateway 发起的请求能够到达后端服务，并返回预期状态码和响应体。

验收应以实际 Server、Gateway、Agent 和后端服务日志为准，不依赖固定脚本、固定 IP 或单一机器环境。
