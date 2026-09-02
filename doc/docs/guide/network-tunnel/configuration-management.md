# AirTunnel 配置管理

AirTunnel Server 可以保存每个 Agent 的目标配置，并在 Agent 首次注册或重新连接时自动补发。修改配置不需要重新打包 Agent。

## 配置版本

Agent 建立连接时，会在 Hello 帧中上报本地已应用的配置版本。Server 只在目标版本更高时下发配置。

- Agent 已经是目标版本：不重复下发。
- Agent 版本较旧：通过现有 mTLS 长连接发送配置快照。
- Agent 离线：目标配置写入持久化目录，下一次连接自动补发。

配置快照只包含运行参数和本地服务声明，不包含证书私钥、CA 密码或 Enrollment Token。

## 管理 API

默认管理前缀为 `/_airtunnel/admin`。管理请求需要满足以下条件之一：

- 使用 ControlPlaneAuth 登录会话；
- 携带 `X-AirTunnel-Admin-Token`；
- 携带 `Authorization: Bearer <token>`。

管理端口由 `ReverseTunnelSettings:Management:BindUrl` 指定，默认是 `https://0.0.0.0:18643`。

查看 Server 状态、在线 Agent、会话和配置版本：

```http
GET /_airtunnel/admin/overview
```

`sessions` 中的关键字段：

| 字段 | 含义 |
| --- | --- |
| `configurationVersion` | Agent 当前已应用的版本 |
| `desiredConfigurationVersion` | Server 期望 Agent 应用的版本 |
| `configurationDeliveryStatus` | `not_started`、`pending`、`acknowledged` 或 `rejected` |
| `configurationDeliveryFailureReason` | 最近一次拒绝或超时原因 |
| `configurationDeliveryAtUtc` | 最近一次下发时间 |

读取和保存 Server 配置：

```http
GET /_airtunnel/admin/config
PUT /_airtunnel/admin/config
Content-Type: application/json
```

请求体必须使用新的 JSON envelope；配置和变更原因都不能再通过裸配置对象或 `X-AirTunnel-Change-Reason` 请求头传递：

```json
{
  "options": {
    "enabled": true,
    "roles": "Server"
  },
  "changeReason": "调整监听器"
}
```

保存前会执行 `ReverseTunnelOptions` 校验。保存成功后会：

1. 写入拆分配置文件；
2. 更新运行期配置；
3. 生成当前配置备份；
4. 写入操作人、原因、版本和差异摘要。

TCP/UDP listener 配置变化会由 Server 运行期服务处理：删除或修改时先停止并等待旧 listener 释放端口，再绑定新配置；新增 listener 在下一次协调周期内启动。

## Agent 目标配置

向在线 Agent 下发配置，或保存离线 Agent 的目标配置：

```http
POST /_airtunnel/admin/clients/configuration-requests
Content-Type: application/json
```

在线 Agent 使用 `sessionId`，离线 Agent 使用 `agentId`。管理页提交完整 Agent 快照，并必须携带读取时的 `expectedVersion`；Server 在同一存储临界区内执行版本比较、ServiceKey/目标冲突校验和全量落盘。版本已变化时返回 `409 configuration_version_conflict`，不会覆盖最新配置；请重新读取后再编辑保存。

```json
{
  "agentId": "branch-agent",
  "expectedVersion": 12,
  "configuration": {
    "agentId": "branch-agent",
    "configuration": {
      "enabled": true,
      "roles": "Agent",
      "agentConnectionCount": 2,
      "maxConcurrentStreams": 256,
      "localServices": [
        {
          "serviceName": "订单服务",
          "publicPath": "/orders",
          "localAddress": "http://127.0.0.1:8080"
        }
      ],
      "tcp": {
        "enabled": true,
        "localServices": [
          {
            "serviceName": "分支 Redis",
            "host": "127.0.0.1",
            "port": 6379,
            "serverListenHost": "0.0.0.0",
            "serverListenPort": 16379
          }
        ]
      },
      "udp": {
        "enabled": true,
        "localServices": [
          {
            "serviceName": "分支 DNS",
            "host": "127.0.0.1",
            "port": 53,
            "serverListenHost": "0.0.0.0",
            "serverListenPort": 1053
          }
        ]
      }
    }
  }
}
```

Agent 会保留本地安全字段，包括 `ServerAddress`、证书路径、CA 配置和 Enrollment 配置。收到配置后先校验 AgentId、版本和 SHA-256，再使用临时文件替换快照；任何一步失败都会返回 `ConfigurationReject`，旧配置继续生效。

查询指定 Agent 的目标配置和在线状态：

```http
GET /_airtunnel/admin/clients/{agentId}/configuration
GET /_airtunnel/admin/clients/{agentId}/configuration-status
```

## Agent 列表、新增与导出

管理页的“Agent 配置”现在以列表为主。列表可按 AgentId 和在线状态筛选，并提供“新增 Agent”和“导出配置包”操作。新增接口只保存 Server 侧可管理的身份与连接参数，Agent 的私钥仍由客户端首次启动时生成：

```http
POST /_airtunnel/admin/clients
Content-Type: application/json

{
  "agentId": "agent-orders",
  "serverAddress": "https://192.168.100.165:8090",
  "agentConnectionCount": 1,
  "heartbeatIntervalSeconds": 15,
  "configurationSyncEnabled": true,
  "configurationSyncIntervalSeconds": 30
}
```

创建成功返回 `201 Created`。点击“导出配置包”会先打开导出参数弹框；填写镜像名、容器名和 Compose 运行配置后提交：

```http
POST /_airtunnel/admin/clients/{agentId}/export
Content-Type: application/json

{
  "imageName": "airtunnel-agent:1.0.0.0",
  "containerName": "airtunnel-agent-orders",
  "runtimeEnvironment": {
    "ASPNETCORE_URLS": "http://+:18543",
    "ASPNETCORE_ENVIRONMENT": "Production",
    "TZ": "Asia/Shanghai"
  }
}
```

导出 ZIP 包含 `config/00-airtunnel-agent.json`、`data/certs/ca.crt`、`docker-compose.yml` 和 `README.md`。弹框中的镜像名、容器名和运行配置会原样写入 `docker-compose.yml` 的 `image`、`container_name` 和 `environment` 节点。镜像名和容器名必须使用小写；运行配置必须是非空 JSON 对象，键和值不能包含换行。包内明确不包含 `agent.key`、`agent.crt`、`ca.key` 或 `ca.pfx`；Agent 首次启动时自行生成私钥并完成证书注册。保留的 GET 导出接口使用默认值，适合脚本调用。

## 管理页面

进入 `/_airtunnel/admin` 后，“Agent 配置”菜单展示 Agent 列表。点击编辑可修改连接数、心跳和配置同步参数；点击导出会打开参数弹框，确认后下载该 Agent 的部署配置包。新建 Agent 成功后也会打开同一个弹框。HTTP/TCP/UDP 的本地目标和对外映射仍在独立的“映射配置”菜单维护，保存时页面提交完整 JSON 快照到上述 API。

页面不会编辑或覆盖 AgentId、角色、ServerAddress、证书、Enrollment、CA 和管理端等本地安全字段。Server 配置页也不再提供 HTTP `Routes`、HTTP `LocalServices`、TCP/UDP `Listeners` 或 TCP/UDP `LocalServices` 的新增/移除入口；这些映射统一在 Agent 配置页维护，Server 总览只读展示派生结果。

## 公开路由与内部键

`ServiceKey` 不再由用户配置，也不作为外部 URL 暴露。保存 Agent 配置时，Server 会覆盖任何请求中携带的 `ServiceKey`，根据规范化路由计算小写 32 位 MD5 作为隧道协议内部标识：

- HTTP：对 `http:{publicPath}` 求 MD5。例如 `GET /orders/api/list` 命中 `publicPath=/orders`，Agent 收到的本地相对路径为 `/api/list`。
- TCP：对 `tcp://{serverListenHost}:{serverListenPort}` 求 MD5。
- UDP：对 `udp://{serverListenHost}:{serverListenPort}` 求 MD5。

因此 HTTP 服务填写服务名称、`publicPath`、可选的 `rewritePath`，以及 Agent 主机可访问的 `protocol`/`host`/`port`。例如：

```json
{
  "ServiceName": "Orders",
  "PublicPath": "/orders",
  "RewritePath": "/tests",
  "Protocol": "http",
  "Host": "127.0.0.1",
  "Port": 5100
}
```

请求 `GET /orders/list` 会通过隧道访问 Agent 的 `http://127.0.0.1:5100/tests/list`。`PublicPath` 也支持末尾通配符，例如 `/api/orders/{path}`；请求 `/api/orders/2026/items` 会捕获 `2026/items`，再按 `RewritePath`（如 `/tests/{path}`）生成目标路径。TCP/UDP 服务填写服务名称、Agent 本地 `host`/`port`，以及 Server 对外 `serverListenHost`/`serverListenPort`。本地端口与 Server 对外端口独立。

该 MD5 仅用于确定性索引，不能视为密码或授权凭据。Agent 仍通过 AgentId 和 mTLS 鉴别。旧的手工 `ServiceKey`、HTTP `virtualHost` 和显式 `Routes` 已不再作为可维护配置；重新保存 Agent 配置后会按本规则生成新键。

HTTP Gateway 使用 `UseReverseTunnelGateway()` 在 Ocelot 前匹配公开路径。命中路径直接调用隧道，不再需要 Consul 或 Ocelot 的虚拟下游主机路由；未命中路径继续交由现有 Ocelot 管道处理。`/_airtunnel`、`/_airgateway` 和 `/_control-plane` 管理路径不会被隧道路由接管。

## 备份、恢复和审计

查看备份和审计记录：

```http
GET /_airtunnel/admin/config/backups
GET /_airtunnel/admin/config/audit
```

恢复指定备份：

```http
POST /_airtunnel/admin/config/backups/{backupName}/restore
Content-Type: application/json
```

恢复原因也必须放在 JSON 请求体中：

```json
{
  "changeReason": "回滚无效路由"
}
```

恢复操作会先备份当前配置，再写入恢复后的配置，并新增一条审计记录。

## 持久化目录

建议将以下目录挂载到持久化磁盘：

```text
/opt/airtunnel/
  configs/
    airtunnel.server.json
    airtunnel.listeners.json
    airtunnel.authorizations.json
    airtunnel.limits.json
    airtunnel.diagnostics.json
    airtunnel.management.json
    airtunnel.agent-configurations.json
  config-backups/
```

如果只挂载容器临时目录，Server 重启后将无法恢复离线 Agent 的目标配置、备份和审计记录。
