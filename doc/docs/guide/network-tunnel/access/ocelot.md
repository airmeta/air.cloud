# Ocelot 隧道接入

Ocelot 隧道接入用于把 AirTunnel 接入到已有 Ocelot Gateway 链路中。入口环境的 Ocelot Gateway 负责接收外部请求并选择是否进入 AirTunnel；后端环境的 Ocelot Gateway 负责在请求穿透后继续按本环境的 Ocelot、Consul、鉴权、白名单和服务治理规则转发到业务服务。

该模式不需要修改 Ocelot 源代码，只需要在入口 Gateway 注册 `ReverseTunnelDelegatingHandler`，并让需要穿透的 Ocelot 路由显式 opt-in。

## 主要作用

Ocelot 隧道接入主要用于多个网络环境下的 HTTP 请求协同运行。入口网络只暴露统一 Gateway，后端网络通过 Agent 主动建立连接；入口 Gateway 收到请求后通过 AirTunnel 送到后端 Gateway，再由后端 Gateway 按本环境的路由和治理规则处理。

常见场景：

- 内外网设备通过公网入口设备向外提供 HTTP 能力，内网设备不需要开放入站端口。
- 开发环境与测试环境之间存在单向访问限制，需要通过统一入口完成接口联调。
- 多个网络治理区之间不能直接互访，但允许某一侧主动连接统一接入层。
- 分支网络、客户现场网络、边缘节点需要被中心系统调用，但中心系统不能直接访问这些网络内的服务地址。
- 后端环境已有 Ocelot、Consul、鉴权、白名单或服务治理规则，穿透后仍希望由后端 Gateway 统一寻址业务服务。

## 接入链路

```text
外部用户 / 上游系统
  -> 入口 Ocelot Gateway
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 Ocelot Gateway
  -> 后端业务服务
```

后端环境的 Agent 可以是独立宿主进程，也可以内置在后端 Ocelot Gateway 进程中。Agent 内置在后端 Gateway 时，Agent 把穿透请求交给后端 Gateway，再由后端 Gateway 按已有 Ocelot 路由寻址业务服务。

## 入口 Gateway 配置

入口 Gateway 负责两个动作：

1. 暴露 AirTunnel Server，接收后端 Agent 主动连接。
2. 在命中指定 Ocelot 路由时，把请求送入 AirTunnel。

### 注册 Ocelot Handler

```csharp
services.AddOcelot(configurationManager)
    .AddReverseTunnelDelegatingHandler()
    .AddConsul()
    .AddPolly();
```

`AddReverseTunnelDelegatingHandler()` 只把 handler 加入 Ocelot 可选处理器列表，不会自动接管普通路由。

### 配置入口 Ocelot 路由

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

### 配置 AirTunnel 路由映射

入口 Gateway 侧使用 `ReverseTunnelSettings:Routes` 把虚拟下游主机映射到逻辑服务键：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Server, Gateway",
    "Routes": [
      {
        "VirtualHost": "tunnel-branch-gateway",
        "ServiceKey": "branch-gateway",
        "TimeoutSeconds": 30
      }
    ],
    "AgentAuthorizations": [
      {
        "AgentId": "branch-gateway-agent",
        "AllowedServiceKeys": [ "branch-gateway" ]
      }
    ]
  }
}
```

`VirtualHost` 必须与入口 Ocelot 路由中的虚拟下游主机一致。`ServiceKey` 是入口 Gateway、AirTunnel Server 和后端 Agent 之间的逻辑服务标识。

Server 侧还需要配置 CA、证书签发、Enrollment 或 Agent 证书材料。证书与密钥配置方式见[配置与安全](../config-security.md)。

## 后端 Gateway 配置

后端 Gateway 负责两个动作：

1. 作为 Agent 主动连接入口 AirTunnel Server。
2. 接收 Agent 转交的 HTTP 请求，并继续按后端 Ocelot 路由转发到业务服务。

### 启用 Agent

后端 Gateway 内启用 Agent 角色，并把 `LocalServices.LocalAddress` 指向后端 Gateway 自己可访问的 HTTP 监听地址：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Agent",
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

如果 Agent 与后端 Gateway 运行在同一进程，`LocalAddress` 应填写该进程对 Agent 可访问的 Gateway HTTP 地址。该地址只在后端环境内部使用，不暴露给入口环境。

### 配置后端 Ocelot 路由

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

## 运行时行为

- 未声明 `ReverseTunnelDelegatingHandler` 的路由继续走 Ocelot 原有下游链路。
- 声明 handler 但未命中 `ReverseTunnelSettings:Routes` 的路由继续走 Ocelot 原有下游链路。
- 命中 AirTunnel 路由后，不访问虚拟下游地址，而是由 `IReverseTunnelInvoker` 发送隧道帧给 Agent。
- 当前 Gateway 进程没有可处理该 `ServiceKey` 的 Agent 会话时，返回 `503 ServiceUnavailable`。
- Agent 调用超时时返回 `504 GatewayTimeout`。
- Agent 返回错误帧时按错误类型返回 `502`、`503`、`413` 或 `429`。
- 请求体超过 `MaxRequestBodyBytes` 时，返回 `413 RequestEntityTooLarge`。
- Gateway 不会预先聚合完整请求体；请求体读取过程中按帧发送给 Agent。
- Agent 收到 `OpenRequest` 后即可启动后端 HTTP 调用，后续请求体帧通过请求流继续写入。
- Agent 收到本地 HTTP 响应头后立即返回响应头，响应体继续按帧流式返回给入口 Gateway。
- 路由超时或客户端取消时，入口 Gateway 会向 Agent 发送 `Cancel` 帧；Agent 会取消本地 HTTP 调用。
- 响应头已经返回给入口 Gateway 后，如果上游客户端断开或响应体被提前释放，入口 Gateway 也会发送 `Cancel` 帧，后端 Agent 不会继续读取本地长响应。

## 配置原则

- 入口 Gateway 的虚拟下游主机只用于 AirTunnel 路由匹配，不应解析到真实业务服务。
- 每条需要穿透的 Ocelot 路由都必须显式声明 `ReverseTunnelDelegatingHandler`。
- 普通 Ocelot + Consul + Polly 路由不受 AirTunnel 影响。
- `Routes[].TimeoutSeconds` 应根据后端服务实际响应时间设置，避免无界等待。
- 后端 Gateway 调用业务服务时，应使用后端环境自己的服务发现、鉴权、白名单和下游地址。
- 如果后端业务服务内部会再次调用 Gateway，应指向后端 Gateway，避免请求绕回入口 Gateway。

## 验收检查

- 后端 Agent 可以主动连接入口 AirTunnel Server。
- 入口 Server 侧可以看到 `branch-gateway-agent` 会话，并且该会话声明了 `branch-gateway`。
- 入口 Ocelot 路由命中 `ReverseTunnelDelegatingHandler`。
- `DownstreamHostAndPorts[].Host` 与 `ReverseTunnelSettings:Routes[].VirtualHost` 完全匹配。
- 后端 Agent 的 `LocalServices[].ServiceKey` 与入口 Gateway 的 `Routes[].ServiceKey` 完全匹配。
- 后端 Gateway 可以按本环境 Ocelot 路由访问真实业务服务。
- 从入口 Gateway 发起请求时，请求路径、查询参数、请求头和请求体可以到达后端业务服务。
