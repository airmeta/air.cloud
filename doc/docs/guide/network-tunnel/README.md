# 网络穿透（AirTunnel）

AirTunnel 是 Air.Cloud 的 HTTP 网络穿透能力。它用于处理“入口环境可以被访问，但后端环境不能被入口环境直接访问”的场景：后端环境中的 Agent 主动连接 Server，Gateway 收到请求后通过已建立的连接把请求转发到 Agent，再由 Agent 调用后端环境中的 HTTP 服务。

实现模块名称为 `Air.Cloud.Modules.ReverseTunnel`，文档和菜单使用能力名称“网络穿透（AirTunnel）”。

## 适用边界

AirTunnel 当前面向 HTTP 服务穿透：

- 支持 Gateway 将 HTTP 请求转发到不可直连环境中的 HTTP/HTTPS 服务。
- 支持 Agent 内嵌在普通 Web 服务、Gateway 或专用宿主进程中。
- 支持 mTLS、证书签发、证书续签、Agent 服务声明授权。
- 支持 HTTP 请求体和响应体流式转发、大小限制、帧分块、并发流限制、超时取消和基础背压。

当前不覆盖通用 TCP、UDP、SOCKS、HTTP CONNECT 或 WebSocket 透传。如果需要暴露数据库、SSH、Redis 等非 HTTP 协议，应等待后续通用网络穿透能力扩展。

## 基本拓扑

```text
外部用户 / 上游系统
  -> 入口 Gateway
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 HTTP 服务或后端 Gateway
```

网络要求：

- 用户或上游系统可以访问入口 Gateway。
- Agent 所在环境可以主动访问 AirTunnel Server。
- AirTunnel Server 不需要主动访问 Agent 所在环境。
- Agent 所在环境内部可以访问被代理的 HTTP 服务。

## 角色说明

| 角色 | 运行位置 | 职责 |
| --- | --- | --- |
| Server | 入口环境或专用接入层 | 接收 Agent gRPC 双向流连接，维护会话，签发和续签 Agent 证书 |
| Gateway | 入口网关 | 根据 Ocelot 路由选择是否把 HTTP 请求送入 AirTunnel |
| Agent | 后端环境 | 主动连接 Server，接收请求帧，调用本地 HTTP 服务并返回响应 |

同一个进程可以同时启用多个角色。例如入口 Gateway 可以同时启用 `Server, Gateway`；后端 Gateway 可以启用 `Agent` 并把请求继续交给后端 Ocelot 路由。

## 接入步骤

1. 在宿主项目引用并注册 `Air.Cloud.Modules.ReverseTunnel`。
2. 在 Server 侧准备 CA 证书、Agent 证书签发策略和 Agent 服务声明授权。
3. 在 Agent 侧准备证书材料挂载路径和可代理的 `LocalServices`。
4. 在入口 Ocelot Gateway 注册 `ReverseTunnelDelegatingHandler`。
5. 在需要穿透的入口 Ocelot 路由中显式声明 `ReverseTunnelDelegatingHandler`。
6. 使用 `ReverseTunnelSettings:Routes` 建立入口虚拟下游主机与后端 `ServiceKey` 的映射。
7. 在后端 Gateway 启用 Agent，并让 `LocalServices.LocalAddress` 指向后端 Gateway 可访问地址。
8. 启动 Server、Agent、Gateway 后验证 Agent 会话、入口 Ocelot 路由和后端业务服务响应。

## 文档索引

1. [应用场景](./scenarios.md)
2. [Server 与 Agent 配置](./server-agent.md)
3. 隧道接入
   - [Ocelot 隧道接入](./access/ocelot.md)
4. [配置与安全](./config-security.md)
5. [验证与排查](./verification.md)
