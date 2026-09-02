# AirTunnel Client 容器部署

该目录用于部署 AirTunnel Agent。Agent 主动连接 Server，不需要在客户端服务器开放公网端口。

## 文件

```text
client/
  Dockerfile
  docker-compose.yml
  config/00-airtunnel-agent.json
  configs/.gitkeep
  data/
    certs/
  logs/
```

## 启动

```bash
docker compose -f docker-compose.yml up -d
```

如果服务器使用旧版 Compose：

```bash
docker-compose -f docker-compose.yml up -d
```

## 必改配置

编辑 `config/00-airtunnel-agent.json`：

| 配置项 | 说明 |
| --- | --- |
| `ReverseTunnelSettings.AgentId` | 客户端身份，必须和 Server 端 `AgentAuthorizations.AgentId` 一致。 |
| `ReverseTunnelSettings.ServerAddress` | Server 控制口地址；生产 Gateway 通过宿主机 `8090` 转发到容器 `18443`，例如 `https://192.168.100.165:8090`。 |
| `ReverseTunnelSettings.AgentConnectionCount` | Agent 到 Server 的并发物理连接数，固定少量目标一般 1-4。 |
| `ReverseTunnelSettings.LocalServices[].ServiceName` | 管理页面中显示的服务名称。 |
| `ReverseTunnelSettings.LocalServices[].PublicPath` | Gateway 对外暴露的 HTTP 路径前缀，例如 `/internet-egress`。 |
| `ReverseTunnelSettings.LocalServices[].Protocol` | 目标协议，支持 `http` 或 `https`。 |
| `ReverseTunnelSettings.LocalServices[].Host` | 该客户端服务器能访问的目标主机，例如 `api.example.com`。 |
| `ReverseTunnelSettings.LocalServices[].Port` | 目标端口，例如 `443`。 |
| `ReverseTunnelSettings.LocalServices[].RewritePath` | 可选的 Gateway 路径重写模板，例如 `/tests` 或 `/tests/{path}`。 |

## 必挂载文件

首次 Enrollment 前，需要放入 CA 公钥证书：

```text
data/certs/ca.crt
```

`agent.crt` 和 `agent.key` 会由 Agent 首次 Enrollment 成功后写入：

```text
data/certs/agent.crt
data/certs/agent.key
```

## 固定目标映射

示例：

```json
{
  "ServiceName": "Internet Egress",
  "PublicPath": "/internet-egress",
  "Protocol": "https",
  "Host": "example.com",
  "Port": 443
}
```

Server/Gateway 侧访问：

```text
http://<gateway-host>:18081/internet-egress/path
```

实际会由客户端访问：

```text
https://example.com/path
```

## 注意事项

- `Protocol`、`Host`、`Port` 组成客户端固定目标；path 由 Server/Gateway 入口请求追加，或由 `RewritePath` 改写。
- 该方案适合少量固定目标，不是通用 HTTP/SOCKS 代理。
- 首次启动且本地没有 `agent.crt` 时，Agent 使用 `data/certs/ca.crt` 的公钥加密随机 GUID；Server 使用同一 CA 私钥解密并在 replay 窗口内拒绝重复 GUID。
