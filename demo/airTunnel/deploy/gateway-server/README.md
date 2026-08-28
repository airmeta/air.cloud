# airGateway 内置 AirTunnel Server 部署

该目录用于把 AirTunnel Server 挂在 airGateway 内运行。它和客户端 `docker-compose.client.yml` 分开维护。

## 必改配置

编辑 `config/20-airtunnel-server.json`：

| 配置项 | 说明 |
| --- | --- |
| `Kestrel:Endpoints:AirTunnel:Url` | Server 控制口监听地址，默认 `https://0.0.0.0:18443`。 |
| `Kestrel:Endpoints:AirTunnel:Certificate` | Server HTTPS 证书路径。 |
| `ReverseTunnelSettings:AgentAuthorizations[].AgentId` | 允许连接的客户端身份。 |
| `ReverseTunnelSettings:AgentAuthorizations[].AllowedServiceKeys` | 允许该客户端声明的固定目标映射。 |
| `ReverseTunnelSettings:Gateway:AccessTokenEnvironmentVariable` | Gateway invoke 入口访问 token 的环境变量名。 |

编辑 `secrets/airtunnel.env`：

```bash
AIR_TUNNEL_CA_PASSWORD=<ca.pfx 密码>
AIR_TUNNEL_GATEWAY_TOKEN=<访问 /_airtunnel/gateway 时要求的 token>
```

## 必挂载文件

```text
data/airtunnel/ca.pfx
data/airtunnel/ca.crt
data/airtunnel/server.crt
data/airtunnel/server.key
```

首次 Enrollment 不需要 token 文件。Agent 只挂载与 Server CA PFX 对应的 `ca.crt`，并用其公钥加密随机 GUID；Server 用 CA 私钥解密并执行 replay 防护。

## 启动

```bash
docker compose -f docker-compose.gateway-server.yml up -d
```
