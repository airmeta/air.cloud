# 网络穿透（AirTunnel）模块

`Air.Cloud.Modules.ReverseTunnel` 是 Air.Cloud 的网络穿透模块，产品能力名称为 AirTunnel。它既可以作为类库接入现有 Gateway 或其它 HTTP Web 应用，也可以通过 `demo/airTunnel` 作为独立产品宿主运行。本文提供接入、配置、验证和运行参考；能力边界与拓扑概览见[网络穿透概览](/guide/network-tunnel/)。

> 当前路由模型已切换为 Agent 配置驱动：HTTP 使用 `LocalServices[].PublicPath`，TCP/UDP 使用 `ServerListenHost/ServerListenPort`，`ServiceKey` 在保存时由系统计算 MD5。Gateway 在 Ocelot 之前按公开路径匹配并转发。旧的 `Routes`、`VirtualHost`、手工 `ServiceKey` 和 `ReverseTunnelDelegatingHandler` 配置不再读取，本文后续如出现旧字段均视为历史说明。

---

## 应用场景

AirTunnel 解决的是 HTTP 服务在单向可达网络中的访问问题。只要后端环境可以主动连接入口环境，就可以通过 AirTunnel 把入口 Gateway 收到的 HTTP 请求送到后端环境处理。

### 内网 HTTP 服务对外访问

内网服务不能开放公网端口，但可以主动访问入口环境。

```text
外部用户
  -> 入口 Gateway
  -> AirTunnel Server
  -> 内网 Agent
  -> 内网 HTTP 服务
```

适用场景：

- 客户现场系统需要被总部平台访问。
- 分支网络只允许出站连接，不允许中心网络直接入站访问。
- 边缘节点主动连接中心节点，由中心节点调用边缘 HTTP 服务。

### 多环境 HTTP 服务互访

两个环境之间只有单向网络可达。例如 A 不能访问 B，但 B 可以访问 A。

```text
A 环境 Gateway
  -> A 环境 AirTunnel Server
  -> B 环境 Agent
  -> B 环境 HTTP 服务
```

适用场景：

- 办公网、客户内网、容器网络之间存在单向访问限制。
- 某个环境不能把服务注册到入口环境的服务发现系统。
- 后端环境地址不稳定，不适合由入口环境直接维护下游地址。

### 后端 Gateway 作为穿透入口

后端环境中可能不止一个业务服务。Agent 可以把请求转给后端 Gateway，再由后端 Gateway 按本环境的 Ocelot、Consul、鉴权、白名单或内部路由规则继续分发。

```text
入口 Gateway
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 Gateway
  -> 后端业务服务
```

这种方式适合后端环境已经有完整网关规则的系统。AirTunnel 只负责跨网络可达性；进入后端 Gateway 后，业务路由仍由后端环境自己的 Gateway 负责。

### 非生产环境使用

在测试、预发、演示或隔离环境中，Agent 可以指向该环境内实际可访问的 HTTP 服务。AirTunnel 不要求固定 IP、固定端口或固定启动脚本，接入方只需要保证：

- Agent 可以访问 AirTunnel Server。
- Agent 的 `LocalServices` 指向目标环境中实际可访问的 HTTP 地址。
- Gateway 能读取 Server 保存的 Agent 路由目录，并按 `PublicPath` 匹配请求。
- 业务服务使用的配置、数据库、鉴权和服务发现来源符合该环境要求。

### 与 DynamicApp 的关系

AirTunnel 不是 DynamicApp 的子功能。两者解决的问题不同：

| 能力 | 解决的问题 |
| --- | --- |
| AirTunnel | 网络不可达时，把 HTTP 请求转发到主动连接的后端环境 |
| DynamicApp | 应用 Mod 的发布、启动、健康检查、切流、排空和回滚 |

DynamicApp 可以部署在 AirTunnel 链路任意一侧，也可以完全不使用 AirTunnel。AirTunnel 也可以转发到普通 HTTP 服务、后端 Gateway 或其他业务进程。

---

## Server 与 Agent 配置

AirTunnel 使用 gRPC 双向流建立 Server 与 Agent 之间的长连接。Agent 主动连接 Server，Server 通过会话表把 Gateway 请求发送给可处理目标 `ServiceKey` 的 Agent。

AirTunnel 支持两种宿主形态：

- 产品形态：直接运行 `demo/airTunnel`，把 AirTunnel 当作独立网络隧道程序部署。
- 类库形态：在现有 Gateway 或其它 HTTP Web 应用中引用 `Air.Cloud.Modules.ReverseTunnel`，由该 Web 应用内嵌承载 Server、Agent 或 Ocelot handler。

独立产品宿主位于：

```text
demo/airTunnel
```

### 注册模块

在需要使用 AirTunnel 的宿主服务中注册模块：

```csharp
services.AddReverseTunnel();
```

该注册会加入配置绑定、路由解析、会话注册表、证书控制面、Agent 出站连接客户端、本地 HTTP 分发器和 Gateway 调用器。

`AddReverseTunnel()` 只注册服务，不会自动开放 gRPC endpoint，也不会自动接管 Ocelot 路由。Server endpoint、Gateway 远程调用入口和 Ocelot handler 都必须由宿主显式配置。

### Server 侧配置

### 管理页面的结构化配置

启用 AirTunnel 管理端后，ControlPlaneAuth 中的 AirTunnel 管理页面会以结构化表单展示 `ReverseTunnelSettings`。对象会按配置分组展开，数组支持新增、删除和逐项编辑；布尔值使用下拉框，数字值使用数字输入框，避免直接编辑 JSON 时破坏层级或类型。

点击“保存配置”后必须在弹框中填写变更原因，页面才会发送保存请求。操作人由管理端统一记录为 `management-page`，变更原因写入配置审计记录。字段旁的“需重启”标记表示该字段由启动阶段绑定、监听器或证书材料决定，保存后需要重启宿主；显示“保存后生效”的字段使用运行期配置访问器，保存后即可影响新请求或下一轮后台循环。

保存前可以点击“校验配置”检查服务端约束。校验和保存都使用与原 JSON 编辑器相同的 `/config/validate` 与 `/config` API，因此不会改变配置文件拆分、备份和审计行为。

Server 侧负责接收 Agent 连接、接收入口 Gateway 的远程调用、管理 Agent 证书，并按 TCP/UDP 配置绑定穿透监听端口。产品宿主配置示例见：

```text
demo/airTunnel/air.cloud.airtunnel/appsettings.Server.example.json
```

如果在自定义宿主中注册 Server endpoint，典型配置为：

```csharp
app.UseRouting();
app.UseReverseTunnelServer();
```

Server gRPC 入口使用 mTLS 校验 Agent 身份。宿主使用 Kestrel 暴露 HTTPS 时，必须允许接收客户端证书；否则 Agent 已携带证书也无法在 Server 侧完成会话注册：

```csharp
services.Configure<KestrelServerOptions>(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        httpsOptions.ClientCertificateValidation = (_, _, _) => true;
    });
});
```

映射 Server endpoint 时会检查：

- `ReverseTunnelSettings:Enabled=true`。
- `ReverseTunnelSettings:TunnelMode` 配置为 `Server`。
- CA PFX 挂载文件存在。
- CA PFX 密码环境变量存在且有值。
- CA PFX 可加载并包含私钥。
- 如果 `Enrollment.Enabled=true`，Server CA PFX 可加载且 `ReplayWindowSeconds` 配置有效。

Server 公开的 gRPC 服务：

| 服务 | 用途 |
| --- | --- |
| `ReverseTunnelTransport.Connect` | Agent 使用 mTLS 建立双向流连接 |
| `ReverseTunnelCertificateManagement.Renew` | Agent 使用当前 mTLS 证书续签 |
| `ReverseTunnelEnrollment.Enroll` | Agent 使用一次性凭据申请首张证书；仅在 `Enrollment.Enabled=true` 时映射 |

Server 同时公开 Gateway 远程调用入口，默认路径为：

```text
/_airtunnel/gateway/{serviceKey}/{**path}
```

入口 Gateway 命中 Ocelot 隧道路由后，会把原始 HTTP method、path、query、headers 和 body 转发到该入口。Server 根据 `{serviceKey}` 从当前进程会话表选择 Agent，再复用已有 AirTunnel HTTP 帧协议转发到后端环境。

### Agent 侧配置

当 `ReverseTunnelSettings:Enabled=true` 且 `TunnelMode=Agent` 时，宿主进程内的 `ReverseTunnelAgentHostedService` 会启动 Agent 出站连接循环。产品宿主配置示例见：

```text
demo/airTunnel/air.cloud.airtunnel/appsettings.Agent.example.json
```

`AgentConnectionCount` 控制单个 Agent 进程向 Server 建立的物理隧道连接数量。默认值为 `1`。调高后仍使用同一个 `AgentId` 和同一套 mTLS 证书作为安全身份，但每条物理连接会在 Server 侧注册为独立会话。入口侧将 `SessionSelectionStrategy` 设置为 `RoundRobin` 时，可以在单 Gateway/Server 进程内把同一个 `ServiceKey` 的请求分摊到这些连接上。

Agent 连接流程：

1. 从 `AgentCertificate:CertificatePath` 和 `AgentCertificate:PrivateKeyPath` 加载 mTLS 客户端证书。
2. 如果证书不存在，且配置了 Enrollment 凭据文件，则向 Server 申请首张证书。
3. 如果证书进入续签窗口，则向 Server 申请续签证书。
4. 使用 `ServerAddress` 打开 gRPC 双向流。
5. 发送 Hello 帧，携带 `AgentId` 和系统根据公开路由生成的 `ServiceKey` 集合。
6. 按 `HeartbeatIntervalSeconds` 周期发送 Ping。
7. 接收 Server 下发的 HTTP 请求帧。
8. 按 `LocalServices` 分发到本地 HTTP 服务。
9. 将本地响应写回 Server。

Agent 连接断开或连接失败后，会按 `AgentReconnectInitialDelaySeconds` 到 `AgentReconnectMaxDelaySeconds` 的配置执行退避重连。

### Agent 本地 HTTP 分发边界

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

### ServiceKey 约定

`ServiceKey` 是 Gateway、Server 和 Agent 之间的内部服务标识，不由用户配置。HTTP 由 `MD5("http:" + PublicPath)` 生成；TCP 和 UDP 分别由 `MD5("tcp://ServerListenHost:ServerListenPort")`、`MD5("udp://ServerListenHost:ServerListenPort")` 生成。Server 侧只使用 Agent 配置目录中的派生结果，`AgentAuthorizations` 如启用也必须填写对应生成值。

多个 Agent 可以声明同一个 `ServiceKey`。Server 默认选择最近活跃的会话；如需改变行为，可通过 `ReverseTunnelSettings:SessionSelectionStrategy` 设置为 `NewestActive`、`OldestActive` 或 `RoundRobin`。`RoundRobin` 只在当前 Server 进程内轮询，不是跨 Server 进程全局负载均衡。

服务展示名称请填写在 `ServiceName`；不要把 IP、端口或临时机器名写入可维护配置中的内部键。

---

## Ocelot 隧道接入（历史说明）

本节保留旧版 Ocelot handler 的背景资料，仅用于理解历史部署；新部署不要按本节配置。当前 Gateway 应使用 `UseReverseTunnelGateway()`，由 Agent 配置目录按 `PublicPath` 直接匹配，Ocelot 不再注册隧道 handler。

Ocelot 隧道接入用于把 AirTunnel 接入到已有 Ocelot Gateway 链路中。入口环境的 Ocelot Gateway 负责接收外部请求并选择是否进入 AirTunnel；后端环境的 Ocelot Gateway 负责在请求穿透后继续按本环境的 Ocelot、Consul、鉴权、白名单和服务治理规则转发到业务服务。

该模式不需要修改 Ocelot 源代码，只需要在入口 Gateway 注册 `ReverseTunnelDelegatingHandler`，并让需要穿透的 Ocelot 路由显式 opt-in。

### 主要作用

Ocelot 隧道接入主要用于多个网络环境下的 HTTP 请求协同运行。入口网络只暴露统一 Gateway，后端网络通过 Agent 主动建立连接；入口 Gateway 收到请求后通过 AirTunnel 送到后端 Gateway，再由后端 Gateway 按本环境的路由和治理规则处理。

常见场景：

- 内外网设备通过公网入口设备向外提供 HTTP 能力，内网设备不需要开放入站端口。
- 开发环境与测试环境之间存在单向访问限制，需要通过统一入口完成接口联调。
- 多个网络治理区之间不能直接互访，但允许某一侧主动连接统一接入层。
- 分支网络、客户现场网络、边缘节点需要被中心系统调用，但中心系统不能直接访问这些网络内的服务地址。
- 后端环境已有 Ocelot、Consul、鉴权、白名单或服务治理规则，穿透后仍希望由后端 Gateway 统一寻址业务服务。

### 接入链路

```text
外部用户 / 上游系统
  -> 入口 Ocelot Gateway
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 Ocelot Gateway
  -> 后端业务服务
```

后端环境的 Agent 可以使用 `demo/airTunnel` 独立运行，也可以以内嵌库方式运行在后端 Web/Gateway 进程中。Agent 可以把 `LocalServices` 指向后端 Ocelot Gateway，也可以直接指向后端业务服务。指向后端 Gateway 时，Agent 把穿透请求交给后端 Gateway，再由后端 Gateway 按已有 Ocelot 路由寻址业务服务。

### 入口 Gateway 配置

入口 Gateway 负责两个动作：

1. 注册 `ReverseTunnelDelegatingHandler`。
2. 在命中指定 Ocelot 路由时，把请求送入 AirTunnel。Server 可以与 Gateway 同进程承载，也可以由独立 `demo/airTunnel` 进程承载。

#### 注册 Ocelot Handler

```csharp
services.AddOcelot(configurationManager)
    .AddReverseTunnelDelegatingHandler()
    .AddConsul()
    .AddPolly();
```

`AddReverseTunnelDelegatingHandler()` 只把 handler 加入 Ocelot 可选处理器列表，不会自动接管普通路由。

#### 配置入口 Ocelot 路由

AirTunnel 路由使用“虚拟下游主机”作为稳定路由键。该地址不会被真实访问，只用于让 `ReverseTunnelDelegatingHandler` 识别要进入 AirTunnel 的请求。

```json
{
  "UpstreamPathTemplate": "/branch/{everything}",
  "DownstreamPathTemplate": "/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "tunnel-branch-gateway", "Port": 80 }
  ],
  "DelegatingHandlers": [ "ReverseTunnelDelegatingHandler" ]
}
```

这里的 `DownstreamHostAndPorts[].Host` 是虚拟主机名，不会被 DNS 解析，也不会建立真实 TCP 连接。

#### 配置 AirTunnel 路由映射

入口 Gateway 侧使用 `ReverseTunnelSettings:Routes` 把虚拟下游主机映射到逻辑服务键：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "None",
    "EnableTunnelProxy": true,
    "Gateway": {
      "RemoteServerAddress": "https://airtunnel-server.example.com:18443",
      "InvokePathPrefix": "/_airtunnel/gateway",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_GATEWAY_TOKEN"
    },
    "Routes": [
      {
        "VirtualHost": "tunnel-branch-gateway",
        "ServiceKey": "branch-gateway",
        "TimeoutSeconds": 30
      }
    ]
  }
}
```

`VirtualHost` 必须与入口 Ocelot 路由中的虚拟下游主机一致。`ServiceKey` 是入口 Gateway、AirTunnel Server 和后端 Agent 之间的逻辑服务标识。

Server 侧还需要配置 `TunnelMode=Server`、CA、证书签发、Enrollment 和 Agent 授权；如果 Server 与 Gateway 同进程，再设置 `EnableTunnelProxy=true`。TCP/UDP 对外 listener 不再在 Server 配置页重复维护，而是从 Agent 配置中的 `Tcp.LocalServices`、`Udp.LocalServices` 自动派生。Server 可以由当前 Gateway 进程以内嵌库方式承载，也可以由 `demo/airTunnel` 独立产品进程承载。证书与密钥配置方式见[配置与安全](../config-security.md)。

### 后端 Gateway 配置

后端 Gateway 只负责本环境内的 HTTP 路由治理。Agent 可以把 `LocalServices` 指向后端 Gateway 地址；请求到达后端 Gateway 后，再由后端 Gateway 按已有 Ocelot、Consul、鉴权、白名单和插件规则转发到业务服务。

#### 启动 Agent

在后端环境启动 AirTunnel Agent，并把 `LocalServices.LocalAddress` 指向后端 Gateway 可访问的 HTTP 监听地址。Agent 可以来自 `demo/airTunnel` 独立产品，也可以来自内嵌库的 Web/Gateway 宿主：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-gateway-agent",
    "ServerAddress": "https://air-tunnel.example.com",
    "LocalServices": [
      {
        "ServiceKey": "branch-gateway",
        "LocalAddress": "http://backend-gateway.internal:8080"
      }
    ]
  }
}
```

`LocalAddress` 只在后端环境内部使用，不暴露给入口环境。它可以是后端 Gateway 地址，也可以是具体业务服务地址；选择后端 Gateway 时，后续寻址继续由后端 Gateway 的 Ocelot 配置负责。

#### 配置后端 Ocelot 路由

请求穿透到后端 Gateway 后，后端 Gateway 按自己的 Ocelot 配置继续寻址业务服务。示例：

```json
{
  "UpstreamPathTemplate": "/orders/{everything}",
  "DownstreamPathTemplate": "/api/orders/{everything}",
  "DownstreamScheme": "http",
  "ServiceName": "order-service",
  "LoadBalancerOptions": {
    "Type": "RoundRobin"
  }
}
```

后端 Ocelot 路由可以继续使用 Consul、固定下游地址、鉴权、白名单、Polly 或其它后端环境已有能力。AirTunnel 不参与后端 Gateway 到业务服务之间的服务发现和负载均衡。

### 运行时行为

- 未声明 `ReverseTunnelDelegatingHandler` 的路由继续走 Ocelot 原有下游链路。
- 声明 handler 但未命中 `ReverseTunnelSettings:Routes` 的路由继续走 Ocelot 原有下游链路；如果 Ocelot 当前 `HttpRequestMessage.Content` 为空，而原始 ASP.NET Core 请求仍携带请求体，handler 会先恢复请求体和 `Content-Type`，再回落到普通下游链路，避免入口 Gateway 代理普通 POST 请求时丢失 body。
- 命中 AirTunnel 路由后，不访问虚拟下游地址，而是由 `IReverseTunnelInvoker` 使用当前进程内会话表，或按 `Gateway.RemoteServerAddress` 调用独立 AirTunnel Server。
- 当前 Server 进程没有可处理该 `ServiceKey` 的 Agent 会话时，返回 `503 ServiceUnavailable`。
- Agent 调用超时时返回 `504 GatewayTimeout`。
- Agent 返回错误帧时按错误类型返回 `502`、`503`、`413` 或 `429`。
- 请求体超过 `MaxRequestBodyBytes` 时，返回 `413 RequestEntityTooLarge`。
- Gateway 不会预先聚合完整请求体；请求体读取过程中按帧发送给 Agent。
- Agent 收到 `OpenRequest` 后即可启动后端 HTTP 调用，后续请求体帧通过请求流继续写入。
- Agent 收到本地 HTTP 响应头后立即返回响应头，响应体继续按帧流式返回给入口 Gateway。
- 路由超时或客户端取消时，入口 Gateway 会向 Agent 发送 `Cancel` 帧；Agent 会取消本地 HTTP 调用。
- 响应头已经返回给入口 Gateway 后，如果上游客户端断开或响应体被提前释放，入口 Gateway 也会发送 `Cancel` 帧，后端 Agent 不会继续读取本地长响应。

### 配置原则

- 入口 Gateway 的虚拟下游主机只用于 AirTunnel 路由匹配，不应解析到真实业务服务。
- 每条需要穿透的 Ocelot 路由都必须显式声明 `ReverseTunnelDelegatingHandler`。
- 普通 Ocelot + Consul + Polly 路由不受 AirTunnel 影响。
- `Routes[].TimeoutSeconds` 应根据后端服务实际响应时间设置，避免无界等待。
- 后端 Gateway 调用业务服务时，应使用后端环境自己的服务发现、鉴权、白名单和下游地址。
- 如果后端业务服务内部会再次调用 Gateway，应指向后端 Gateway，避免请求绕回入口 Gateway。

### 验收检查

- 后端 Agent 可以主动连接入口 AirTunnel Server。
- 入口 Server 侧可以看到 `branch-gateway-agent` 会话，并且该会话声明了 `branch-gateway`。
- 入口 Ocelot 路由命中 `ReverseTunnelDelegatingHandler`。
- `DownstreamHostAndPorts[].Host` 与 `ReverseTunnelSettings:Routes[].VirtualHost` 完全匹配。
- 后端 Agent 的 `LocalServices[].ServiceKey` 与入口 Gateway 的 `Routes[].ServiceKey` 完全匹配。
- 后端 Gateway 可以按本环境 Ocelot 路由访问真实业务服务。
- 从入口 Gateway 发起请求时，请求路径、查询参数、请求头和请求体可以到达后端业务服务。

---

## 配置与安全

AirTunnel 配置节为 `ReverseTunnelSettings`。模块默认禁用；启用 Server/Agent 连接端时设置 `TunnelMode`，是否拦截 Gateway 动态路由由 `EnableTunnelProxy` 单独控制。

### 最小配置

Server 侧：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Server",
    "EnableTunnelProxy": false,
    "HeartbeatIntervalSeconds": 15,
    "SessionIdleTimeoutSeconds": 45,
    "MaxConcurrentStreams": 128,
    "MaxRequestBodyBytes": 10485760,
    "MaxResponseBodyBytes": 10485760,
    "MaxFramePayloadBytes": 65536,
    "OutboundFrameQueueCapacity": 1024,
    "SessionSelectionStrategy": "NewestActive",
    "Gateway": {
      "InvokePathPrefix": "/_airtunnel/gateway",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_GATEWAY_TOKEN"
    },
    "CertificateAuthority": {
      "CertificatePath": "/mnt/secrets/airtunnel/ca.pfx",
      "CertificatePasswordEnvironmentVariable": "AIRTUNNEL_CA_PASSWORD",
      "TrustedIssuerPath": "/mnt/secrets/airtunnel/ca.crt",
      "AgentCertificateLifetimeDays": 30,
      "RenewalWindowDays": 7,
      "RenewalOverlapHours": 24
    },
    "AgentAuthorizations": [
      {
        "AgentId": "agent-order",
        "AllowedServiceKeys": [ "order-service" ]
      }
    ]
  }
}
```

入口 Gateway 侧：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "None",
    "EnableTunnelProxy": true,
    "Gateway": {
      "RemoteServerAddress": "https://air-tunnel.example.com",
      "InvokePathPrefix": "/_airtunnel/gateway",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_GATEWAY_TOKEN"
    },
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

Agent 侧：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "agent-order",
    "ServerAddress": "https://air-tunnel.example.com",
    "AgentConnectionCount": 1,
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
      "PrivateKeySizeBits": 2048
    }
  }
}
```

示例中的域名、路径和服务名需要替换为实际部署值。首次 Enrollment 使用 `TrustedIssuerPath` 对应 CA 公钥加密随机 GUID，Server 使用同一 CA PFX 私钥解密并防重放。不要把私钥、证书内容或 CA 密码写入普通配置文件、数据库或 Consul。

### Agent 配置派生 Server 路由

HTTP `LocalServices` 现在以 Agent 配置为唯一配置入口。Server 会从挂载目录中的 Agent 目标配置快照汇总服务，并自动生成只读路由索引，因此 Server 配置页不再需要重复维护 HTTP `LocalServices`。

当 Agent 的 `LocalServices` 中出现以下配置时：

```json
{
  "ServiceKey": "order-service",
  "LocalAddress": "http://127.0.0.1:6101"
}
```

Server 会自动生成：

```text
VirtualHost: tunnel-order-service
ServiceKey: order-service
TimeoutSeconds: 30
```

如果该 Agent 配置同时包含同一 `ServiceKey` 的 `Routes` 项，则使用其中的 `VirtualHost` 和 `TimeoutSeconds`，否则使用 `tunnel-{ServiceKey}` 和 30 秒默认值。多个 Agent 可以声明同一个 `ServiceKey`，Server 只生成一个逻辑入口，实际连接仍由 `SessionSelectionStrategy` 选择。

Agent 配置保存成功后，Server 会立即刷新派生索引；Server 重启时会从 `Management.ConfigDirectory` 下的 Agent 配置快照恢复。管理总览中的“Agent 派生路由”区域可用于确认当前索引。

### 配置项说明

| 配置项 | 作用 |
| --- | --- |
| `Enabled` | 是否启用模块，默认 `false` |
| `TunnelMode` | 连接端模式：Server 接收 Agent，Agent 主动连接 Server；`None` 仅用于管理或独立远程 Gateway |
| `EnableTunnelProxy` | 是否由当前 Gateway 进程在 Ocelot 之前拦截动态隧道路由 |
| `AgentId` | Agent 稳定身份，必须与证书身份匹配 |
| `ServerAddress` | Agent 主动连接的 Server 地址 |
| `Gateway.RemoteServerAddress` | 入口 Gateway 调用独立 AirTunnel Server 的地址 |
| `Gateway.InvokePathPrefix` | Gateway 到 Server 的 HTTP 调用入口前缀，默认 `/_airtunnel/gateway` |
| `Gateway.AccessTokenEnvironmentVariable` | Gateway 到 Server 的可选访问令牌环境变量名 |
| `AgentConnectionCount` | 单个 Agent 进程主动建立的物理隧道连接数，默认 `1`，范围 `1-64` |
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
| `Routes` | 兼容旧配置；Agent 配置包含同一 `ServiceKey` 的显式入口时可由 Agent 派生，Server 配置页不再编辑 HTTP 路由 |
| `LocalServices` | Agent 侧 `ServiceKey` 到本地 HTTP 地址的唯一配置入口；Server 从 Agent 目标配置自动汇总 |
| `Tcp.LocalServices` | Agent 侧 TCP 本地目标和 Server 对外 `ServerListenHost`/`ServerListenPort`；Server 自动派生 TCP listener |
| `Udp.LocalServices` | Agent 侧 UDP 本地目标和 Server 对外 `ServerListenHost`/`ServerListenPort`；Server 自动派生 UDP listener |
| `AgentAuthorizations` | Server 侧 Agent 可声明 `ServiceKey` 的授权规则 |
| `CertificateAuthority` | Server 侧 CA 挂载路径和签发策略 |
| `Enrollment` | Agent 首次注册开关及加密 GUID replay 窗口 |
| `AgentCertificate` | Agent 侧证书、私钥和信任 CA 的挂载路径 |
| `Diagnostics` | 当前进程内诊断 HTTP API 和快照采集配置，默认关闭 |

### 多 Agent 选择策略

多个 Agent 可以声明相同的 `ServiceKey`。同一个 Agent 进程也可以通过 `AgentConnectionCount` 为相同 `ServiceKey` 建立多条物理连接。Server 会根据 `SessionSelectionStrategy` 从当前进程内可用会话中选择一个会话：

| 策略 | 行为 | 适用场景 |
| --- | --- | --- |
| `NewestActive` | 选择最近活跃的会话。默认值，保持兼容行为。 | 本地联调、单活 Agent、希望新连接接管流量 |
| `OldestActive` | 选择最早活跃的会话。 | 希望已有连接保持优先，避免新连接立即接管 |
| `RoundRobin` | 按 `ServiceKey` 在当前 Gateway/Server 进程内轮询会话。 | 单实例内多 Agent 或同 Agent 多物理连接做基础分摊 |

`RoundRobin` 只作用于当前 Gateway/Server 进程内已经注册的会话。单实例压测或生产运行中，如果单条 gRPC 双向流成为瓶颈，可以先增加 `AgentConnectionCount`，并将入口侧 `SessionSelectionStrategy` 设置为 `RoundRobin`，再根据压测结果调整 `MaxConcurrentStreams`、`OutboundFrameQueueCapacity` 和超时策略。

### HTTP 流式转发与运行指标

HTTP 请求进入 AirTunnel 后，请求体不会在 Gateway 侧预先聚合为完整 `byte[]`。Gateway 会在读取 `HttpContent` 的同时按 `MaxFramePayloadBytes` 拆分为 `RequestBody` 帧；Agent 收到 `OpenRequest` 后即可启动本地 HTTP 调用，并通过有界队列把后续请求体帧写入本地请求流。

HTTP 内容头会随请求元数据一起穿透，包括 `Content-Type`、`Content-Encoding`、`Content-Language`、`Content-Location`、`Content-MD5` 与 `Content-Range`。在 Ocelot handler 阶段如果下游 `HttpRequestMessage.Content` 为空，但当前 ASP.NET Core 请求仍存在内容头、非零 `Content-Length` 或 `Transfer-Encoding: chunked`，AirTunnel 会从当前请求上下文恢复请求体和内容头；因此 JSON、表单和其它依赖媒体类型绑定的 POST/PUT/PATCH 请求不会因为进入隧道而变成无媒体类型请求。

本地 HTTP 响应使用 `ResponseHeadersRead` 模式处理。Agent 在响应头可用后立即写回 `ResponseHeaders`，响应体按读取块持续写回 `ResponseBody`；Gateway 收到响应头后即可向上游返回 `HttpResponseMessage`，响应体继续通过流式 `HttpContent` 输出。

大小、超时和取消策略：

- `MaxRequestBodyBytes` 在 Gateway 读取请求体并发送帧时累计校验；超限后返回 `413 RequestEntityTooLarge`，并向 Agent 发送 `Cancel` 帧。
- `MaxResponseBodyBytes` 在 Agent 读取本地响应体、Gateway 接收响应体帧时都会累计校验；超限后返回 `response_body_too_large` 错误并终止本次 HTTP 流。Gateway 侧发现响应体超限时会向 Agent 发送 `Cancel` 帧，避免 Agent 继续读取后端大响应。
- `Routes[].TimeoutSeconds` 覆盖一次穿透调用从发起到收到响应头的等待时间；超时后 Gateway 返回 `504 GatewayTimeout`，并向 Agent 发送 `Cancel` 帧。
- 客户端取消请求时，Gateway 会向 Agent 发送 `Cancel` 帧；Agent 收到后取消本地 HTTP 调用并关闭请求体队列。
- Gateway 已收到 Agent 响应头后，如果上游提前释放响应体、响应写出失败或读取被取消，Gateway 仍会向 Agent 发送 `Cancel` 帧，避免长响应继续占用 Agent 本地 HTTP 调用和隧道流。
- `MaxConcurrentStreams` 限制单个 Agent 会话的并发 HTTP 流数量；超过限制的新请求返回 `429 TooManyRequests`。
- `AgentConnectionCount` 大于 `1` 时，单个 Agent 进程会注册多条会话；每条会话都有独立的 gRPC 写入路径、出站帧队列和 `MaxConcurrentStreams` 限额。
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

### 诊断快照 API

AirTunnel 可以在当前进程内开放只读诊断 API，用于在没有完整观测平台时查询该进程的隧道状态。诊断 API 不要求当前进程一定包含 `Server` 角色；Agent、Gateway、Server 进程都可以显式映射。每次请求都会从 `AppCore.Configuration` 读取 `ReverseTunnelSettings:Diagnostics:Enabled`。关闭时返回 `404`，开启时返回当前进程快照。

在普通 AirTunnel Server 中，`UseReverseTunnelServer()` 会随 gRPC Server endpoint 一起映射诊断 API：

```csharp
app.UseRouting();
app.UseReverseTunnelServer();
```

在 Agent-only 或 Gateway-only 进程中，可以只映射诊断 API：

```csharp
app.UseRouting();
app.UseReverseTunnelDiagnostics();
```

如果宿主进程还有签名校验、白名单、业务鉴权或 Ocelot 中间件，诊断 endpoint 应放在这些业务中间件之前。这样 `/airtunnel/diagnostics/*` 会直接返回诊断响应，不会落入业务路由或被普通业务签名规则拦截；其他业务请求仍继续走原有管道。

配置示例：

```json
{
  "ReverseTunnelSettings": {
    "Diagnostics": {
      "Enabled": true,
      "PathPrefix": "/airtunnel/diagnostics",
      "SnapshotCollectionIntervalSeconds": 15
    }
  }
}
```

配置项：

| 配置项 | 默认值 | 说明 |
| --- | ---: | --- |
| `Diagnostics:Enabled` | `false` | 是否开放诊断 HTTP API |
| `Diagnostics:PathPrefix` | `/airtunnel/diagnostics` | 诊断 API 路径前缀；路径属于路由映射配置，修改后需要重启 |
| `Diagnostics:SnapshotCollectionIntervalSeconds` | `15` | 后台刷新诊断快照的周期，范围 `1-3600` 秒 |

接口地址：

| 接口 | 返回内容 |
| --- | --- |
| `GET /airtunnel/diagnostics/snapshot` | 配置摘要、指标快照和会话快照 |
| `GET /airtunnel/diagnostics/metrics` | 当前进程内指标快照 |
| `GET /airtunnel/diagnostics/sessions` | 当前进程内 Agent 会话快照 |

返回语义：

- API 返回的是当前 Agent/Gateway/Server 进程内的最近一次采集快照，不跨多个 Gateway 实例聚合。
- `Diagnostics.Enabled` 是运行期配置；从 `false` 改为 `true` 后，已存在的诊断 endpoint 会从返回 `404` 变为返回快照。
- 后台采集周期由 `SnapshotCollectionIntervalSeconds` 控制，采集服务每轮都会重新读取该配置；服务刚启动且尚未完成后台采集时，首次查询会同步生成一次快照。
- `metrics.total` 是当前进程内所有 ServiceKey/AgentId 的聚合计数。
- `metrics.dimensions` 按 `ServiceKey` 和 `AgentId` 拆分计数。
- `sessions` 显示当前 Server/Gateway 进程内已连接 Agent 会话、声明的 `ServiceKey`、最近活跃时间、空闲秒数和活跃 HTTP 流数量；Agent-only 进程没有 Server 侧会话注册表时，该列表可以为空。

采样开销：

- 业务请求路径只维护内存计数和当前会话状态，不访问数据库、配置中心、日志文件或外部观测平台。
- 请求计数、字节数、活跃流等使用原子计数更新；按 `ServiceKey` 和 `AgentId` 拆分的维度会写入内存字典。
- 后台采样按周期复制当前计数、会话列表并做少量排序，默认 15 秒一次。正常 ServiceKey 和 Agent 数量下，对 HTTP 穿透吞吐影响很低。
- 如果同一进程内存在大量 `ServiceKey`、大量 Agent 或非常短的采样周期，快照复制和排序会增加 CPU 与内存瞬时开销。生产运行建议不要把采样周期调到 1 秒，除非正在短时间排障。

安全注意事项：

- 诊断 API 会暴露 `AgentId`、`ServiceKey`、会话数量、活跃流和错误统计，不应默认开放到公网。
- 在入口 Gateway 上启用时，应通过网关白名单、内网访问、鉴权中间件或反向代理限制访问来源。
- 该 API 用于运行排障和联调验证，不替代 OpenTelemetry、Prometheus、日志平台或告警系统。

### 证书和凭据

证书、私钥和 CA 密码只应通过挂载磁盘或 Secret 管理。配置中只保存路径和环境变量名称。

不得写入配置中心的内容：

- CA 私钥。
- CA PFX 内容。
- CA PFX 密码。
- Agent 私钥。
- Agent 证书内容。
- 证书吊销状态明细。

### Enrollment

`Enrollment` 用于新 Agent 首次申请证书，或失效 Agent 恢复证书。Agent 从 `TrustedIssuerPath` 读取 Server CA 公钥，生成随机 GUID 并使用 RSA-OAEP-SHA256 加密；Server 使用同一 CA PFX 私钥解密，并在 `ReplayWindowSeconds` 窗口内拒绝重复 GUID。流程不需要 token 文件或凭据目录。

### 证书续签

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

### 安全默认值

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

---

## 验证与排查

AirTunnel 验证应从四个边界依次确认：Server endpoint、Agent 会话、Gateway 路由、后端 HTTP 服务。

### Server endpoint

确认 Server 进程已启动，并且配置满足：

- `ReverseTunnelSettings:Enabled=true`。
- `ReverseTunnelSettings:TunnelMode` 配置为 `Server`。
- `UseReverseTunnelServer()` 或 `MapReverseTunnelServer()` 已被调用。
- CA PFX 文件可读，且密码环境变量存在。
- 如果启用 Enrollment，Server CA PFX 私钥可加载，且 `ReplayWindowSeconds` 在有效范围内。

常见错误：

| 现象 | 检查项 |
| --- | --- |
| Server 启动时报配置校验失败 | CA 文件路径、CA 密码环境变量、`Enrollment.Enabled` 和证书材料 |
| Agent 连接返回 `Unauthenticated` | Agent 客户端证书是否由 Server 信任 CA 签发，证书 AgentId 是否匹配 |
| Agent 连接返回 `PermissionDenied` | `AgentAuthorizations` 是否允许该 Agent 声明对应 `ServiceKey` |

### Agent 会话

确认 Agent 进程已启动，并且可以主动访问 `ServerAddress`。

Agent 侧配置至少应包含：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
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

### Gateway 路由

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
    "TunnelMode": "None",
    "EnableTunnelProxy": true,
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

### 后端 HTTP 服务

在 Agent 所在环境中直接访问 `LocalServices.LocalAddress`，确认后端服务可用：

```bash
curl -i http://order-service.internal:8080/health
```

再从入口 Gateway 访问对应上游路径：

```bash
curl -i https://gateway.example.com/orders/health
```

两次调用应到达同一个后端服务。不要把响应中的密钥、票据、Token 或其它敏感材料复制到日志、文档或聊天记录中。

### 状态码排查

| 状态码 | 含义 | 排查方向 |
| --- | --- | --- |
| `503 ServiceUnavailable` | Gateway 当前没有可用 Agent 会话，或会话已关闭 | Agent 是否连接，`ServiceKey` 是否一致，Server 是否摘除了空闲会话 |
| `504 GatewayTimeout` | Gateway 等待 Agent 响应超时 | `Routes[].TimeoutSeconds`、后端服务响应时间、Agent 到后端服务网络 |
| `499` | 上游客户端取消或响应体提前释放 | 客户端连接、入口代理超时、响应体是否被调用方提前关闭 |
| `413 RequestEntityTooLarge` | 请求体超过 `MaxRequestBodyBytes` | 上游请求大小、Gateway 配置 |
| `429 TooManyRequests` | 单 Agent 会话并发流达到 `MaxConcurrentStreams` | 并发量、Agent 连接数、并发限制配置 |
| `502 BadGateway` | Agent 返回错误或响应处理失败 | Agent 日志、本地 HTTP 服务异常、响应体是否超过 `MaxResponseBodyBytes` |

### 指标和链路检查

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

### 部署验收

AirTunnel 部署后，应至少完成以下验收：

- Server 可以加载 CA 文件和签发配置。
- Agent 可以完成首次 Enrollment 或使用已有证书建立 mTLS 连接。
- Server 侧可以看到 Agent 会话和对应 `ServiceKey`。
- Gateway 路由命中 `ReverseTunnelDelegatingHandler` 后，请求进入 AirTunnel。
- Agent 所在环境可以访问 `LocalServices.LocalAddress` 指向的后端 HTTP 服务。
- 从入口 Gateway 发起的请求能够到达后端服务，并返回预期状态码和响应体。

验收应以实际 Server、Gateway、Agent 和后端服务日志为准，不依赖固定脚本、固定 IP 或单一机器环境。

---

## 压测与运行参数

AirTunnel HTTP 穿透上线前，应分别压测后端服务直连、后端 Gateway 直连，以及入口 Gateway 经 AirTunnel 的完整链路。目标是确认稳定容量、P95/P99 延迟、错误率、超时率、取消率和 Agent 并发流占用均处于可解释范围。

### PowerShell 压测脚本

脚本位置：

```powershell
.\src\modules\Air.Cloud.Modules.ReverseTunnel\tools\airtunnel-http-benchmark.ps1
```

GET 示例：

```powershell
.\src\modules\Air.Cloud.Modules.ReverseTunnel\tools\airtunnel-http-benchmark.ps1 `
  -TargetUrl "http://gateway.example.com/orders/health" `
  -Method GET `
  -WarmupSeconds 10 `
  -DurationSeconds 60 `
  -Concurrency 64 `
  -TimeoutSeconds 30 `
  -OutputJson ".\tmp\airtunnel-benchmark.json"
```

POST 示例：

```powershell
.\src\modules\Air.Cloud.Modules.ReverseTunnel\tools\airtunnel-http-benchmark.ps1 `
  -TargetUrl "http://gateway.example.com/orders" `
  -Method POST `
  -BodyText '{"id":1}' `
  -ContentType "application/json" `
  -Header "X-Trace-Id: benchmark-001" `
  -WarmupSeconds 10 `
  -DurationSeconds 120 `
  -Concurrency 128
```

输出包含 `requestsPerSecond`、`averageMs`、`p50Ms` 至 `p99Ms`、`statusCounts` 和 `errorCounts`。

### 压测阶段

| 阶段 | 并发 | 时长 | 目标 |
| --- | ---: | ---: | --- |
| 基线 | 1 / 8 / 16 | 60 秒 | 验证路由正确且无明显错误码 |
| 容量摸底 | 32 / 64 / 128 | 120 秒 | 观察 P95/P99、`429`、`504` 和后端错误 |
| 稳定性 | 目标并发的 70% | 15-30 分钟 | 观察内存、连接数、活跃流、取消和超时 |
| 极限保护 | 高于目标并发 20%-50% | 5-10 分钟 | 验证并发限制、超时和大小限制是否生效 |

每个阶段至少记录入口 Gateway、AirTunnel Server、Agent、后端 Gateway 或业务服务的 CPU、内存和连接数，以及 `aircloud_reverse_tunnel_active_streams`、`aircloud_reverse_tunnel_request_failures_total`、`aircloud_reverse_tunnel_backpressure_waits_total`、`aircloud_reverse_tunnel_timeouts_total` 与 `aircloud_reverse_tunnel_cancellations_total`。

### 运行参数建议

| 配置 | 起始建议 | 调整依据 |
| --- | --- | --- |
| `MaxConcurrentStreams` | 单 Agent 目标并发的 1.2-1.5 倍 | 出现 `429` 且后端资源充足时调高；Agent 或后端接近瓶颈时保持或调低 |
| `AgentConnectionCount` | `1`；单条连接成为瓶颈时按 `2 / 4 / 8` 逐级压测 | 单实例吞吐受单 gRPC 双向流写入或队列限制时调高；同时把入口侧 `SessionSelectionStrategy` 设置为 `RoundRobin` |
| `OutboundFrameQueueCapacity` | `MaxConcurrentStreams * 8`，且不低于 `1024` | `backpressure_waits_total` 持续增长时评估带宽、Agent 读取速度和响应体大小 |
| `MaxFramePayloadBytes` | `65536` | 大响应且分帧 CPU 开销明显时可调高；网络抖动或单帧内存压力明显时调低 |
| `MaxRequestBodyBytes` | 按接口最大请求体设置并保留 20%-50% 余量 | 防止入口请求体异常放大占用隧道 |
| `MaxResponseBodyBytes` | 按接口最大响应体设置并保留 20%-50% 余量 | 防止后端大响应持续占用 Agent 和隧道 |
| `Routes[].TimeoutSeconds` | 大于后端 P99，通常 15-60 秒 | `504` 增加时先确认后端耗时，再决定是否调高 |
| `SessionIdleTimeoutSeconds` | `HeartbeatIntervalSeconds * 3` 或更高 | 网络抖动导致误摘除会话时调高 |

### 结果判断

可进入灰度的最低条件是：目标并发下 `2xx/3xx` 比例稳定；`429` 仅在超过设计并发时出现；`504` 与后端 P99 或入口代理超时一致；`backpressure_waits_total` 不持续线性增长；客户端取消时 Agent 本地 HTTP 调用能释放；响应体超限时返回 `response_body_too_large` 并取消 Agent 侧调用。

### 单实例容量估算

单实例容量应按“入口 Gateway/Server 进程 + Agent 进程 + 后端 Gateway 或业务服务”的完整链路压测结果估算。先记录直连后端服务、后端 Gateway 直连、AirTunnel 完整链路三组数据，再比较瓶颈位置。

如果后端直连能力明显高于 AirTunnel 完整链路，并且入口 Gateway/Server CPU、内存、网络和后端服务都未饱和，优先按以下顺序调整：

1. 将入口侧 `SessionSelectionStrategy` 设置为 `RoundRobin`。
2. 将 Agent 侧 `AgentConnectionCount` 从 `1` 逐级提高到 `2 / 4 / 8`，每次调整后重新压测 C50、C100、C200、C300、C500。
3. 根据 `429` 情况调整 `MaxConcurrentStreams`。
4. 根据 `backpressure_waits_total` 与响应体大小调整 `OutboundFrameQueueCapacity` 和 `MaxFramePayloadBytes`。

容量结论应以稳定阶段的 P95/P99、错误率、超时率、取消率和资源占用为准，不以瞬时峰值作为生产容量。
