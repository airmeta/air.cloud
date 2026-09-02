# AirTunnel

AirTunnel 是基于 `Air.Cloud.Modules.ReverseTunnel` 的独立网络穿透产品宿主。它用于承载类似 frp 的 Server/Agent 运行形态，可以处理 HTTP、TCP 和 UDP 三类穿透请求。

## 能力边界

- HTTP：独立 Server 的 `UseReverseTunnelGateway()` 按 Agent 配置中的公开路径直接匹配并转发到已连接 Agent；不需要 Ocelot。外部 Gateway 仍可通过 `/_airtunnel/gateway` 调用 Server。
- TCP：AirTunnel Server 绑定 TCP 监听端口，把外部 TCP 连接转发到 Agent 所在网络内的 TCP 服务。
- UDP：AirTunnel Server 绑定 UDP 监听端口，按客户端端点维护 flow，把 datagram 转发到 Agent 所在网络内的 UDP 服务。
- 证书：Agent 通过 mTLS 连接 Server，Server 可签发和续签 Agent 证书。
- 诊断：可按配置开放只读诊断 API。

## 运行项目

```powershell
dotnet run --project .\air.cloud.airtunnel\air.cloud.airtunnel.csproj -- --urls "https://0.0.0.0:18443"
```

常用配置文件：

- `air.cloud.airtunnel/appsettings.Server.example.json`：Server 侧示例。
- `air.cloud.airtunnel/appsettings.Agent.example.json`：Agent 侧示例。
- `air.cloud.airtunnel/appsettings.Full.example.json`：Gateway 与 Server 同进程的本地验证示例；Agent 连接端需使用独立的 Agent 配置。

## 与 Gateway 内嵌模式的关系

`Air.Cloud.Modules.ReverseTunnel` 仍然可以作为类库被现有 Gateway 或其它 HTTP Web 应用引用。该模式适合把 AirTunnel Server endpoint 或 Ocelot handler 直接挂在已有 Web 应用内。

AirTunnel 管理页面、管理 API 和内置页面资源由 `Air.Cloud.Modules.ReverseTunnel` 提供。该宿主只负责注册类库、提供 ControlPlaneAuth 授权适配和显式调用 `MapReverseTunnelServer()`；不要在宿主目录复制 Tunnel 管理页面。

`demo/airTunnel` 是产品级独立宿主，适合把 AirTunnel 作为单独进程部署、迁移和扩容。Server 宿主的 HTTP 隧道入口默认监听容器端口 `8080`；与 AirGateway 同机部署时，发布模板将其映射为 `18083`，避免占用 Gateway 的 `18081`。Agent mTLS 接入仍监听 `18443`。
