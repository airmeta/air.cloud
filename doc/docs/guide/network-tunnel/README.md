# 网络穿透（AirTunnel）

AirTunnel 是 Air.Cloud 的网络穿透能力。它用于处理“入口环境可以被访问，但后端环境不能被入口环境直接访问”的场景：后端环境中的 Agent 主动连接 Server，入口 Gateway 或 TCP/UDP listener 收到请求后通过已建立的连接把流量转发到 Agent，再由 Agent 调用后端环境中的本地服务。

实现模块名称为 `Air.Cloud.Modules.ReverseTunnel`，文档和菜单使用能力名称“网络穿透（AirTunnel）”。接入方可以直接运行 `demo/airTunnel` 作为独立产品，也可以在现有 Gateway 或其它 HTTP Web 应用中引用该模块库。

## 适用边界

AirTunnel 当前面向 HTTP、TCP 和 UDP 穿透：

- 支持 Gateway 将 HTTP 请求转发到不可直连环境中的 HTTP/HTTPS 服务。
- 支持 TCP listener 将入口 TCP 连接转发到后端环境的 TCP 服务。
- 支持 UDP listener 按客户端端点维护 flow，并把 UDP datagram 转发到后端环境的 UDP 服务。
- 支持独立 `demo/airTunnel` 产品宿主进程承载 Server、Agent、TCP listener 和 UDP listener。
- 支持作为类库内嵌到现有 Gateway 或其它 HTTP Web 应用中。
- 支持 mTLS、证书签发、证书续签、Agent 服务声明授权。
- 支持 HTTP 请求体和响应体流式转发、大小限制、帧分块、并发流限制、超时取消和基础背压。

当前不覆盖 SOCKS、HTTP CONNECT 或 WebSocket 透传。TCP/UDP 穿透只负责字节流或 datagram 转发，不解析数据库、SSH、Redis 等上层协议。

## 基本拓扑

```text
外部用户 / 上游系统
  -> 入口 Gateway
  -> 独立 AirTunnel Server
  -> 后端环境 Agent
  -> 后端 HTTP 服务或后端 Gateway
```

TCP/UDP 链路：

```text
外部客户端
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 TCP/UDP 服务
```

网络要求：

- 用户或上游系统可以访问入口 Gateway。
- Agent 所在环境可以主动访问 AirTunnel Server。
- AirTunnel Server 不需要主动访问 Agent 所在环境。
- Agent 所在环境内部可以访问被代理的 HTTP 服务。

## 角色说明

| 角色 | 运行位置 | 职责 |
| --- | --- | --- |
| Server | 独立 AirTunnel 进程，或引用库的 Web/Gateway 进程 | 接收 Agent gRPC 双向流连接，维护会话，签发和续签 Agent 证书，承载 Gateway 远程调用入口和 TCP/UDP listener |
| Gateway | 入口 Gateway | 在 Ocelot 之前按 Agent 配置的公开路径匹配 HTTP 请求并直接送入 AirTunnel。可与 Server 同进程，也可远程调用独立 AirTunnel Server |
| Agent | 独立 AirTunnel 进程，或引用库的后端 Web/Gateway 进程 | 主动连接 Server，接收请求帧，调用本地 HTTP/TCP/UDP 服务并返回响应 |

## 两种使用方式

### 直接使用 AirTunnel 产品

产品宿主位于：

```text
demo/airTunnel
```

这种方式适合把 AirTunnel 当作类似 frp 的独立网络隧道程序部署。它可以单独承载 Server、Agent、HTTP 远程调用入口、TCP listener、UDP listener 和诊断接口。

### 作为库接入 Web 应用

现有 Gateway 或其它 HTTP Web 应用可以引用 `Air.Cloud.Modules.ReverseTunnel`，并在宿主中注册 `AddReverseTunnel()`、`UseReverseTunnelServer()`、`UseReverseTunnelGateway()` 等扩展。`UseReverseTunnelGateway()` 必须位于 Ocelot 之前。该方式适合在已有 Web 应用中直接复用 AirTunnel 能力。

## 接入步骤

1. 使用 `demo/airTunnel` 启动独立 Server，或者在现有 Web/Gateway 应用中以库方式承载 Server。
2. 在 Server 侧准备 CA 证书、Agent 证书签发策略和 Agent 服务声明授权。
3. 使用 `demo/airTunnel` 启动独立 Agent，或者在后端 Web/Gateway 应用中以库方式启用 Agent，并准备证书材料挂载路径和可代理的 `LocalServices`。
4. 在入口 Gateway 的 Ocelot 之前注册 `UseReverseTunnelGateway()`。
5. 在 Agent 配置页为 HTTP 服务填写公开路径，例如 `/orders` 和本地地址；无需在 Consul 或 Ocelot 添加虚拟下游主机。
6. Server 保存时会基于公开路径或 TCP/UDP Server listener 端点生成内部 `ServiceKey`；它不出现在外部 URL 中。
7. 如果 Gateway 与 Server 不在同一进程，在入口 Gateway 配置 `ReverseTunnelSettings:Gateway:RemoteServerAddress` 指向独立 AirTunnel Server；如果同进程承载，则可不配置该地址。
8. 如果需要 TCP/UDP 穿透，在 Agent 配置页分别添加本地 TCP/UDP 服务，并填写 Server 对外监听地址和端口；Server 自动派生 listener，无需重复配置。
9. 启动 Server、Agent、Gateway 后验证 Agent 会话、公开 HTTP 路径、TCP/UDP listener 和后端服务响应。

## 详细文档

- [应用场景](./scenarios.md)
- [部署手册](./deployment.md)
- [Server 与 Agent 配置](./server-agent.md)
- [Ocelot 隧道接入](./access/ocelot.md)
- [配置与安全](./config-security.md)
- [配置同步](./configuration-synchronization.md)
- [开发排程](./development-plan.md)
- [验证与排查](./verification.md)
- [压测报告与运行参数](./performance.md)
- [模块完整说明](/guide/air-cloud-core/modules/reverse-tunnel.html)
