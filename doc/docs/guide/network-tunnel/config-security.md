# 配置与安全

AirTunnel 配置节为 `ReverseTunnelSettings`。模块默认禁用，必须显式设置 `Enabled=true` 和对应 `Roles` 后才会建立连接或处理 Gateway 路由。

## 最小配置

Server + Gateway 侧：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Server, Gateway",
    "HeartbeatIntervalSeconds": 15,
    "SessionIdleTimeoutSeconds": 45,
    "MaxConcurrentStreams": 128,
    "MaxRequestBodyBytes": 10485760,
    "MaxResponseBodyBytes": 10485760,
    "MaxFramePayloadBytes": 65536,
    "OutboundFrameQueueCapacity": 1024,
    "SessionSelectionStrategy": "NewestActive",
    "CertificateAuthority": {
      "CertificatePath": "/mnt/secrets/airtunnel/ca.pfx",
      "CertificatePasswordEnvironmentVariable": "AIRTUNNEL_CA_PASSWORD",
      "TrustedIssuerPath": "/mnt/secrets/airtunnel/ca.crt",
      "AgentCertificateLifetimeDays": 30,
      "RenewalWindowDays": 7,
      "RenewalOverlapHours": 24
    },
    "Routes": [
      {
        "VirtualHost": "tunnel-order-service",
        "ServiceKey": "order-service",
        "TimeoutSeconds": 30
      }
    ],
    "AgentAuthorizations": [
      {
        "AgentId": "agent-order",
        "AllowedServiceKeys": [ "order-service" ]
      }
    ]
  }
}
```

Agent 侧：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Agent",
    "AgentId": "agent-order",
    "ServerAddress": "https://air-tunnel.example.com",
    "HeartbeatIntervalSeconds": 15,
    "AgentReconnectInitialDelaySeconds": 5,
    "AgentReconnectMaxDelaySeconds": 60,
    "MaxRequestBodyBytes": 10485760,
    "MaxResponseBodyBytes": 10485760,
    "MaxFramePayloadBytes": 65536,
    "LocalServices": [
      {
        "ServiceKey": "order-service",
        "LocalAddress": "http://order-service.internal:8080"
      }
    ],
    "AgentCertificate": {
      "CertificatePath": "/var/lib/air-cloud/airtunnel/agent.cer",
      "PrivateKeyPath": "/var/lib/air-cloud/airtunnel/agent.key",
      "TrustedIssuerPath": "/var/lib/air-cloud/airtunnel/ca.cer",
      "EnrollmentCredentialPath": "/mnt/secrets/airtunnel/enrollment.token",
      "PrivateKeySizeBits": 2048
    }
  }
}
```

示例中的域名、路径和服务名需要替换为实际部署值。不要把私钥、证书内容、CA 密码或 Enrollment token 明文写入普通配置文件、数据库或 Consul。

## 配置项说明

| 配置项 | 作用 |
| --- | --- |
| `Enabled` | 是否启用模块，默认 `false` |
| `Roles` | 当前进程角色，可组合 `Server`、`Gateway`、`Agent` |
| `AgentId` | Agent 稳定身份，必须与证书身份匹配 |
| `ServerAddress` | Agent 主动连接的 Server 地址 |
| `HeartbeatIntervalSeconds` | Agent 心跳间隔 |
| `SessionIdleTimeoutSeconds` | Server 侧会话空闲摘除阈值 |
| `MaxConcurrentStreams` | 单个 Agent 会话最大并发 HTTP 请求流 |
| `MaxRequestBodyBytes` | 单次 HTTP 请求体最大字节数 |
| `MaxResponseBodyBytes` | 单次 HTTP 响应体最大字节数 |
| `MaxFramePayloadBytes` | 单个数据帧最大 payload 字节数 |
| `OutboundFrameQueueCapacity` | 单个 Agent 会话出站帧队列容量 |
| `SessionSelectionStrategy` | 多个 Agent 声明相同 `ServiceKey` 时的会话选择策略 |
| `AgentReconnectInitialDelaySeconds` | Agent 重连初始等待秒数 |
| `AgentReconnectMaxDelaySeconds` | Agent 重连最大退避秒数 |
| `Routes` | Gateway 侧虚拟下游主机到 `ServiceKey` 的映射 |
| `LocalServices` | Agent 侧 `ServiceKey` 到本地 HTTP 地址的映射 |
| `AgentAuthorizations` | Server 侧 Agent 可声明 `ServiceKey` 的授权规则 |
| `CertificateAuthority` | Server 侧 CA 挂载路径和签发策略 |
| `Enrollment` | Agent 首次注册或证书恢复的凭据目录 |
| `AgentCertificate` | Agent 侧证书、私钥和信任 CA 的挂载路径 |

## 多 Agent 选择策略

多个 Agent 可以声明相同的 `ServiceKey`。Server 会根据 `SessionSelectionStrategy` 从当前进程内可用会话中选择一个会话：

| 策略 | 行为 | 适用场景 |
| --- | --- | --- |
| `NewestActive` | 选择最近活跃的会话。默认值，保持兼容行为。 | 本地联调、单活 Agent、希望新连接接管流量 |
| `OldestActive` | 选择最早活跃的会话。 | 希望已有连接保持优先，避免新连接立即接管 |
| `RoundRobin` | 按 `ServiceKey` 在当前 Gateway/Server 进程内轮询会话。 | 同一进程内多个 Agent 做基础分摊 |

`RoundRobin` 不跨 Gateway 进程共享状态；如果部署了多个入口 Gateway，每个进程会维护自己的轮询计数。需要跨进程全局负载均衡时，应在入口层、Agent 多连接策略或后续共享会话注册表中处理。

## HTTP 流式转发与运行指标

HTTP 请求进入 AirTunnel 后，请求体不会在 Gateway 侧预先聚合为完整 `byte[]`。Gateway 会在读取 `HttpContent` 的同时按 `MaxFramePayloadBytes` 拆分为 `RequestBody` 帧；Agent 收到 `OpenRequest` 后即可启动本地 HTTP 调用，并通过有界队列把后续请求体帧写入本地请求流。

本地 HTTP 响应使用 `ResponseHeadersRead` 模式处理。Agent 在响应头可用后立即写回 `ResponseHeaders`，响应体按读取块持续写回 `ResponseBody`；Gateway 收到响应头后即可向上游返回 `HttpResponseMessage`，响应体继续通过流式 `HttpContent` 输出。

大小、超时和取消策略：

- `MaxRequestBodyBytes` 在 Gateway 读取请求体并发送帧时累计校验；超限后返回 `413 RequestEntityTooLarge`，并向 Agent 发送 `Cancel` 帧。
- `MaxResponseBodyBytes` 在 Agent 读取本地响应体、Gateway 接收响应体帧时都会累计校验；超限后返回 `response_body_too_large` 错误并终止本次 HTTP 流。Gateway 侧发现响应体超限时会向 Agent 发送 `Cancel` 帧，避免 Agent 继续读取后端大响应。
- `Routes[].TimeoutSeconds` 覆盖一次穿透调用从发起到收到响应头的等待时间；超时后 Gateway 返回 `504 GatewayTimeout`，并向 Agent 发送 `Cancel` 帧。
- 客户端取消请求时，Gateway 会向 Agent 发送 `Cancel` 帧；Agent 收到后取消本地 HTTP 调用并关闭请求体队列。
- Gateway 已收到 Agent 响应头后，如果上游提前释放响应体、响应写出失败或读取被取消，Gateway 仍会向 Agent 发送 `Cancel` 帧，避免长响应继续占用 Agent 本地 HTTP 调用和隧道流。
- `MaxConcurrentStreams` 限制单个 Agent 会话的并发 HTTP 流数量；超过限制的新请求返回 `429 TooManyRequests`。
- `OutboundFrameQueueCapacity` 是 Server 发往 Agent 的有界帧队列容量；队列满时写入方等待，形成背压。

AirTunnel 使用 `System.Diagnostics.Metrics` 暴露指标。Meter 名称为：

```text
Air.Cloud.Modules.ReverseTunnel
```

AirTunnel 也使用 `System.Diagnostics.ActivitySource` 暴露链路追踪。ActivitySource 名称与 Meter 一致：

```text
Air.Cloud.Modules.ReverseTunnel
```

Gateway 命中 AirTunnel 路由后会创建 `airtunnel.gateway.invoke` activity，并写入以下常用 tag：

| Tag | 含义 |
| --- | --- |
| `reverse_tunnel.service_key` | AirTunnel 逻辑服务键 |
| `reverse_tunnel.virtual_host` | 入口 Ocelot 路由使用的虚拟下游主机 |
| `reverse_tunnel.agent_id` | 本次调用选中的 Agent |
| `reverse_tunnel.session_id` | 本次调用选中的 Server 会话 |
| `http.request.method` | 上游 HTTP 方法 |
| `url.path` | 上游 path/query |
| `http.response.status_code` | Gateway 返回给上游的状态码 |
| `error.type` | 失败错误码 |
| `error.message` | 不包含敏感材料的失败说明 |

如果上游请求没有携带 `traceparent`，Gateway 会把当前 activity 的 W3C `traceparent` 注入到穿透请求头中；如果存在 `tracestate` 也会一并传递。Agent 本地 HTTP 分发会把这些头转发给 `LocalServices` 指向的后端 HTTP 服务。已有的 `traceparent` 不会被覆盖。

当前指标：

| 指标 | 含义 |
| --- | --- |
| `aircloud_reverse_tunnel_requests_total` | HTTP 穿透请求总数 |
| `aircloud_reverse_tunnel_request_failures_total` | HTTP 穿透失败总数，包含 `reason` 标签 |
| `aircloud_reverse_tunnel_request_duration_ms` | HTTP 穿透请求耗时 |
| `aircloud_reverse_tunnel_request_bytes_total` | Gateway 已发送的请求体字节数 |
| `aircloud_reverse_tunnel_response_bytes_total` | Gateway 已接收的响应体字节数 |
| `aircloud_reverse_tunnel_active_streams` | 当前活跃 HTTP 流数量 |
| `aircloud_reverse_tunnel_backpressure_waits_total` | 帧队列满导致写入等待的次数 |
| `aircloud_reverse_tunnel_cancellations_total` | 客户端取消导致的穿透取消次数 |
| `aircloud_reverse_tunnel_timeouts_total` | 路由超时导致的穿透取消次数 |

## 证书和凭据

证书、私钥和一次性 Enrollment token 只应通过挂载磁盘或 Secret 管理。配置中只保存路径和环境变量名称。

不得写入配置中心的内容：

- CA 私钥。
- CA PFX 内容。
- CA PFX 密码。
- Agent 私钥。
- Agent 证书内容。
- Enrollment token 明文。
- 证书吊销状态明细。

## Enrollment

`Enrollment` 用于新 Agent 首次申请证书，或失效 Agent 恢复证书。

凭据文件名规则：

```text
sha256(lowercase(trim(agentId))) + CredentialFileExtension
```

文件内容是一次性 token 明文。`ConsumeOnUse=true` 时，token 验证成功后会被重命名为 `.used-{ticks}` 文件，防止重复签发。

## 证书续签

长期身份采用 mTLS。Server 作为内部 CA，负责签发、续签和吊销 Agent 证书。

续签流程：

1. Agent 读取当前本地客户端证书。
2. Agent 判断证书是否进入 `RenewalWindowDays`。
3. 进入窗口后，Agent 用本地私钥生成新的 CSR。
4. Agent 使用当前客户端证书建立 mTLS 调用 `ReverseTunnelCertificateManagement.Renew`。
5. Server 校验证书链、有效期、Client Auth EKU、吊销状态和 AgentId。
6. Server 只使用证书中的 AgentId 签发下一张证书。
7. Agent 成功后原子替换本地证书文件。

`Hello.AgentId` 不能作为可信身份来源。Server 以 mTLS 客户端证书中的 AgentId 为准。

## 安全默认值

- `Enabled` 默认 `false`。
- 心跳间隔限制在 5 到 300 秒。
- 会话空闲摘除阈值至少是心跳间隔的 2 倍。
- 单会话并发流限制在 1 到 4096。
- 单次请求体和响应体限制在 1 KiB 到 1 GiB。
- 单帧 payload 限制在 1 KiB 到 1 MiB。
- 单会话出站帧队列容量限制在 16 到 65536。
- Agent 重连初始间隔限制在 1 到 300 秒。
- Agent 最大退避间隔不小于初始间隔且不超过 3600 秒。
- 虚拟主机匹配不区分大小写，并移除结尾点。
- Agent 本地 HTTP 目标只来自 `LocalServices`。
- Gateway 路由必须显式声明 `ReverseTunnelDelegatingHandler`。
