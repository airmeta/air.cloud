# Server 与 Agent 配置

AirTunnel 使用 gRPC 双向流建立 Server 与 Agent 之间的长连接。Agent 主动连接 Server，Server 通过会话表把 Gateway 请求发送给可处理目标 `ServiceKey` 的 Agent。

## 注册模块

在需要使用 AirTunnel 的宿主服务中注册模块：

```csharp
services.AddReverseTunnel();
```

该注册会加入配置绑定、路由解析、会话注册表、证书控制面、Agent 出站连接客户端、本地 HTTP 分发器和 Gateway 调用器。

`AddReverseTunnel()` 只注册服务，不会自动开放 gRPC endpoint，也不会自动接管 Ocelot 路由。Server endpoint 和 Ocelot handler 都必须由宿主显式配置。

## Server 侧配置

Server 侧负责接收 Agent 连接和管理 Agent 证书。典型宿主配置：

```csharp
app.UseRouting();
app.UseReverseTunnelServer();
```

映射 Server endpoint 时会检查：

- `ReverseTunnelSettings:Enabled=true`。
- `ReverseTunnelSettings:Roles` 包含 `Server`。
- CA PFX 挂载文件存在。
- CA PFX 密码环境变量存在且有值。
- CA PFX 可加载并包含私钥。
- 如果 `Enrollment.Enabled=true`，Enrollment 凭据目录存在。

Server 公开的 gRPC 服务：

| 服务 | 用途 |
| --- | --- |
| `ReverseTunnelTransport.Connect` | Agent 使用 mTLS 建立双向流连接 |
| `ReverseTunnelCertificateManagement.Renew` | Agent 使用当前 mTLS 证书续签 |
| `ReverseTunnelEnrollment.Enroll` | Agent 使用一次性凭据申请首张证书；仅在 `Enrollment.Enabled=true` 时映射 |

## Agent 侧配置

当 `ReverseTunnelSettings:Enabled=true` 且 `Roles` 包含 `Agent` 时，宿主进程内的 `ReverseTunnelAgentHostedService` 会启动 Agent 出站连接循环。

Agent 连接流程：

1. 从 `AgentCertificate:CertificatePath` 和 `AgentCertificate:PrivateKeyPath` 加载 mTLS 客户端证书。
2. 如果证书不存在，且配置了 Enrollment 凭据文件，则向 Server 申请首张证书。
3. 如果证书进入续签窗口，则向 Server 申请续签证书。
4. 使用 `ServerAddress` 打开 gRPC 双向流。
5. 发送 Hello 帧，携带 `AgentId` 和 `LocalServices.ServiceKey` 集合。
6. 按 `HeartbeatIntervalSeconds` 周期发送 Ping。
7. 接收 Server 下发的 HTTP 请求帧。
8. 按 `LocalServices` 分发到本地 HTTP 服务。
9. 将本地响应写回 Server。

Agent 连接断开或连接失败后，会按 `AgentReconnectInitialDelaySeconds` 到 `AgentReconnectMaxDelaySeconds` 的配置执行退避重连。

## Agent 本地 HTTP 分发边界

Agent 只允许把请求分发到 `ReverseTunnelSettings:LocalServices` 声明的目标。

安全边界：

- 目标根地址只来自 `LocalServices`。
- `LocalServices.LocalAddress` 必须是绝对 HTTP/HTTPS 地址。
- `PathAndQuery` 必须以 `/` 开头。
- `PathAndQuery` 不能是绝对 URI。
- `Host`、`Content-Length`、`Connection`、`Transfer-Encoding`、`Upgrade` 等 hop-by-hop 头不会转发给本地服务。
- 请求体大小受 `MaxRequestBodyBytes` 限制。
- 响应体大小受 `MaxResponseBodyBytes` 限制。
- 请求体和响应体会按 `MaxFramePayloadBytes` 拆分为多个隧道数据帧。

## ServiceKey 约定

`ServiceKey` 是 Gateway、Server 和 Agent 之间的逻辑服务标识。

- Gateway 侧 `Routes[].ServiceKey` 表示某个虚拟下游主机要转发到哪个逻辑服务。
- Agent 侧 `LocalServices[].ServiceKey` 表示本 Agent 可以代理哪个逻辑服务。
- Server 侧 `AgentAuthorizations` 可以限制某个 Agent 允许声明的 `ServiceKey`。

多个 Agent 可以声明同一个 `ServiceKey`。Server 默认选择最近活跃的会话；如需改变行为，可通过 `ReverseTunnelSettings:SessionSelectionStrategy` 设置为 `NewestActive`、`OldestActive` 或 `RoundRobin`。`RoundRobin` 只在当前 Gateway/Server 进程内轮询，不是跨进程全局负载均衡。

建议使用稳定、可读、与部署地址无关的名称，例如 `order-service`、`payment-api`、`customer-gateway`。不要把 IP、端口、临时机器名或环境私有路径写入 `ServiceKey`。
