# 部署手册

本文说明如何把 AirTunnel 作为独立网络穿透程序部署。独立端位于 `demo/airTunnel`，可以按角色分别部署 Server 与 Agent，也可以在本地验证时单进程同时启用 Server 和 Agent。

AirTunnel 也可以作为类库内嵌到 Gateway 或其它 Web 应用中。类库接入方式见[网络穿透（AirTunnel）模块](/guide/air-cloud-core/modules/reverse-tunnel.html)；本文只描述独立端部署。

## 部署拓扑

HTTP 穿透常见拓扑（独立 Server）：

```text
外部用户 / 上游系统
  -> AirTunnel Server（UseReverseTunnelGateway）
  -> AirTunnel Agent
  -> 后端 Gateway 或后端 HTTP 服务
```

TCP / UDP 穿透常见拓扑：

```text
外部 TCP/UDP 客户端
  -> AirTunnel Server listener
  -> AirTunnel Agent
  -> Agent 所在网络内的 TCP/UDP 服务
```

网络方向要求：

| 方向 | 要求 |
| --- | --- |
| 外部用户到入口 Gateway | HTTP 穿透需要可达 |
| 入口 Gateway 到 AirTunnel Server | HTTP 穿透需要可达 |
| Agent 到 AirTunnel Server | 必须可达，Agent 主动建立 gRPC 双向流 |
| AirTunnel Server 到 Agent | 不要求主动可达 |
| Agent 到本地服务 | 必须可达 |
| 外部 TCP/UDP 客户端到 AirTunnel Server listener | 使用 TCP/UDP 穿透时需要可达 |

## 部署产物

在源码环境发布独立端：

```powershell
dotnet publish .\demo\airTunnel\air.cloud.airtunnel\air.cloud.airtunnel.csproj -c Release -o .\.deploy\airtunnel
```

发布目录中需要保留：

- `air.cloud.airtunnel.exe` 或 `air.cloud.airtunnel.dll`
- `appsettings.json`
- 运行时依赖 DLL
- Server 证书和 CA 的挂载路径
- Agent 证书、私钥和可信 CA 的挂载路径

示例配置来源：

| 文件 | 用途 |
| --- | --- |
| `demo/airTunnel/air.cloud.airtunnel/appsettings.Server.example.json` | Server 独立端 |
| `demo/airTunnel/air.cloud.airtunnel/appsettings.Agent.example.json` | Agent 独立端 |
| `demo/airTunnel/air.cloud.airtunnel/appsettings.Full.example.json` | 本地单进程验证 |

部署时把对应示例复制为发布目录下的 `appsettings.json`，再替换地址、端口和证书路径。`ServiceKey` 不需要配置，由 Agent 公开路由自动生成。

## Windows Agent 组合运行包

Server 管理端导出的 Agent ZIP 只包含该 Agent 的运行配置和 CA 公钥。Windows 运行包需要再合并同一版本的 `win-x64` 自包含发布目录。仓库提供 `Package-AirTunnelWindowsAgent.ps1`，脚本会在自身目录创建 `certificates`、`publish-input\win-x64` 和 `publishs` 三个目录；每次调用都会按使用人、版本、AgentId 和时间戳生成一个新的独立目录和 ZIP。

先发布 Windows 运行时：

```powershell
dotnet publish .\demo\airTunnel\air.cloud.airtunnel\air.cloud.airtunnel.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\.deploy\airtunnel-win-x64
```

把 Server 导出的 Agent ZIP 放到脚本同级的 `certificates` 目录，把发布结果复制到脚本同级的 `publish-input\win-x64`，然后直接运行脚本：

```powershell
.\demo\airTunnel\scripts\Package-AirTunnelWindowsAgent.ps1
```

脚本依次询问使用人名称、版本号，再按证书包修改时间倒排列出 `certificates` 中的 ZIP 并让你输入数字选择。`publish-input\win-x64` 是固定输入目录，`publishs` 是固定输出目录。脚本会：

- 把发布结果写入运行包的 `app` 目录；
- 把 Server 导出的 `config/00-airtunnel-agent.json` 写入 `app/config`；
- 将扁平的 Server 导出配置包装为宿主读取的 `ReverseTunnelSettings` 配置节，确保 Agent 模式和 Server 地址在 Windows 宿主中生效；
- 把 `data/certs/ca.crt` 写入 `app/data/certs`；
- 将容器使用的 `/app/data/certs/...` 路径转换为 Windows 运行目录下的相对路径；
- 生成 `start-agent.ps1`、`README-Windows.md` 和 `airtunnel-agent.windows.manifest.json`；
- 检查导出包不包含 `agent.key`、`agent.crt`、`ca.key` 或 `ca.pfx`；
- 同时在 `publishs` 下生成可分发的同名 ZIP；不会覆盖上一次发包结果。

给另一个人打包时把新的 Agent 导出 ZIP 放入 `certificates` 后重新运行即可。启动方解压 ZIP 后在根目录执行：

```powershell
.\start-agent.ps1
```

如果脚本同级或 `publish-input\win-x64` 中存在 `nssm.exe`，运行包会附带后台服务脚本。需要安装为 Windows 后台服务时，在管理员 PowerShell 中执行 `.\install-agent-service.ps1`；卸载时执行 `.\uninstall-agent-service.ps1`。脚本会把 NSSM 的工作目录设为 `app`，因此配置中的相对证书路径可以直接使用。修改配置或替换运行包后需要重启该服务，才能让宿主重新读取 `ReverseTunnelSettings`。

Agent 首次启动仍需要能够访问 Server，并由 Server 根据导出时的 `AgentId` 完成 enrollment；`agent.crt` 和 `agent.key` 会写入运行包的 `app/data/certs`，不会预置在分发 ZIP 中。若服务显示 Running 但在线列表没有 Agent，应先检查 `app/logs/service.stdout.log` 中是否出现 `Agent enrollment succeeded` 或连接重试日志，并确认 `agent.crt`、`agent.key` 已生成。

## 目录规划

建议把程序、配置、证书和日志分开：

```text
/opt/airtunnel/
  app/                 # 发布后的程序目录
  config/              # appsettings.json
  server/
    certs/             # server.pfx、ca.pfx、ca.crt
  agent/
    branch-agent/
      agent.crt
      agent.key
      ca.crt
  logs/
```

Windows 可以使用类似目录：

```text
D:\airtunnel\
  app\
  config\
  server\certs\
  agent\branch-agent\
  logs\
```

不要把 CA 密码或 Agent 私钥内容写入 Consul、数据库或普通说明文档。配置文件只保存路径和环境变量名。

## Server 配置

独立 Server 的 HTTP 隧道入口由 `UseReverseTunnelGateway()` 提供，不依赖 Ocelot。除 Agent mTLS 接入端口外，必须额外监听一个普通 HTTP 端口，例如：

```json
{
  "Kestrel": {
    "Endpoints": {
      "GatewayHttp": {
        "Url": "http://0.0.0.0:8080",
        "Protocols": "Http1AndHttp2"
      },
      "AirTunnel": {
        "Url": "https://0.0.0.0:18443"
      }
    }
  }
}
```

请求 `http://<server>:8080/<Agent公开路径>` 时，Server 会从持久化 Agent 配置中匹配公开路径，并通过当前 Server 进程内的 Agent 会话转发。未命中路由的请求继续执行宿主后续管道。

Server 最小配置结构：

```json
{
  "Urls": "https://0.0.0.0:18443",
  "Kestrel": {
    "Endpoints": {
      "AirTunnel": {
        "Url": "https://0.0.0.0:18443",
        "Protocols": "Http1AndHttp2",
        "Certificate": {
          "Path": "/opt/airtunnel/server/certs/server.pfx",
          "Password": ""
        }
      }
    }
  },
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Server",
    "EnableTunnelProxy": false,
    "ServerCertificate": {
      "CommonName": "192.168.100.156",
      "SubjectAlternativeNames": [
        "192.168.100.156",
        "localhost",
        "air-gateway"
      ]
    },
    "SessionSelectionStrategy": "RoundRobin",
    "Gateway": {
      "InvokePathPrefix": "/_airtunnel/gateway",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_GATEWAY_TOKEN"
    },
    "CertificateAuthority": {
      "CertificatePath": "/opt/airtunnel/server/certs/ca.pfx",
      "CertificatePasswordEnvironmentVariable": "AIR_TUNNEL_CA_PASSWORD",
      "AgentCertificateLifetimeDays": 30,
      "RenewalWindowDays": 7,
      "RenewalOverlapHours": 24
    },
    "Enrollment": {
      "Enabled": true,
      "ReplayWindowSeconds": 300
    },
    "AgentAuthorizations": [],
    "Diagnostics": {
      "Enabled": true,
      "PathPrefix": "/_airtunnel/diagnostics",
      "SnapshotPeriodSeconds": 5
    }
  }
}
```

关键配置：

| 配置 | 说明 |
| --- | --- |
| `Urls` / `Kestrel.Endpoints` | Server 对外监听地址。gRPC 需要 HTTP/2，Gateway 远程调用入口需要 HTTP/1.1 或 HTTP/2 |
| `TunnelMode` | Server 端固定配置为 `Server` |
| `EnableTunnelProxy` | Gateway 与 Server 同进程时配置为 `true`，独立 Server 配置为 `false` |
| `Gateway.InvokePathPrefix` | 入口 Gateway 调用 Server 的 HTTP 前缀，默认 `/_airtunnel/gateway` |
| `Gateway.AccessTokenEnvironmentVariable` | Gateway 到 Server 的访问令牌环境变量名；配置后请求必须携带匹配 token |
| `ServerCertificate.CommonName` | Server TLS 证书主题名称；迁移主机时同步修改 |
| `ServerCertificate.SubjectAlternativeNames` | Server TLS 证书 SAN 列表，必须包含 Agent 实际访问 Server 使用的 IP 或 DNS 名称 |
| `CertificateAuthority.CertificatePath` | Server 用于签发 Agent 证书的 CA PFX 路径 |
| `CertificateAuthority.CertificatePasswordEnvironmentVariable` | CA PFX 密码所在环境变量 |
| `Enrollment.ReplayWindowSeconds` | 同一加密 GUID 的 replay 防护窗口，默认 300 秒 |
| `AgentAuthorizations` | 可选的 AgentId 白名单；ServiceKey 由 Agent 公开路由自动生成 |

Server 环境变量：

```powershell
$env:AIR_TUNNEL_CA_PASSWORD = "<ca-pfx-password>"
$env:AIR_TUNNEL_GATEWAY_TOKEN = "<gateway-to-server-token>"
```

Linux：

```bash
export AIR_TUNNEL_CA_PASSWORD='<ca-pfx-password>'
export AIR_TUNNEL_GATEWAY_TOKEN='<gateway-to-server-token>'
```

### 迁移时重新生成 Server 证书

Server 的 TLS 证书由部署文件挂载，程序不会根据当前主机 IP 自动重签。迁移 Server 时，先修改 `ReverseTunnelSettings.ServerCertificate` 中的 `CommonName` 和 `SubjectAlternativeNames`，再使用仓库脚本按同一 CA 重新生成 `server.crt`、`server.key` 和可选的 `server.pfx`：

```powershell
.\demo\airTunnel\scripts\Generate-AirTunnelServerCertificate.ps1 `
  -ServerConfig .\config\20-airtunnel-server.json `
  -CertificateDirectory .\data\airtunnel `
  -CaCertificatePath .\data\airtunnel\ca.crt `
  -CaPrivateKeyPath .\data\airtunnel\ca.key `
  -Force
```

如果配置了 `ASPNETCORE_Kestrel__Certificates__Default__Password`，脚本会使用该环境变量生成 `server.pfx`；否则仍会生成 PEM 证书和私钥，适用于配置中使用 `Path` 与 `KeyPath` 的 Kestrel endpoint。脚本会在覆盖前创建带时间戳的证书备份目录。替换证书后重启 Server，并确认实际 endpoint 返回的 SAN 包含新的 Server 地址：

```bash
openssl s_client -connect <server-ip>:18443 -showcerts </dev/null 2>/dev/null |
  openssl x509 -noout -subject -issuer -ext subjectAltName
```

## Server 远程管理端

AirTunnel Server 可以单独开放管理端口，用于查看运行状态、管理 TCP/UDP listener、调整授权与限制策略、吊销证书和踢掉在线 session。管理端不承载隧道控制流量，建议与控制端口 `18443` 分开：

“在线 Agent”按 `AgentId` 聚合当前会话；同一 Agent 存在多个物理连接时，当前配置版本、下发状态和失败原因会按唯一值显示一次，不会因为多个 session 重复拼接同一个版本号。需要排查每条物理连接时，到“会话与指标”查看完整 session 列表。

```text
控制端口: https://0.0.0.0:18443
管理端口: https://0.0.0.0:18643
```

Kestrel 需要增加独立 endpoint：

```json
{
  "Kestrel": {
    "Endpoints": {
      "AirTunnel": {
        "Url": "https://0.0.0.0:18443",
        "Protocols": "Http1AndHttp2",
        "Certificate": {
          "Path": "/opt/airtunnel/server/certs/server.pfx",
          "Password": ""
        }
      },
      "AirTunnelManagement": {
        "Url": "https://0.0.0.0:18643",
        "Protocols": "Http1AndHttp2",
        "Certificate": {
          "Path": "/opt/airtunnel/server/certs/server.pfx",
          "Password": ""
        }
      }
    }
  }
}
```

管理端配置：

```json
{
  "ReverseTunnelSettings": {
    "Management": {
      "Enabled": true,
      "BindUrl": "https://0.0.0.0:18643",
      "PathPrefix": "/_airtunnel/admin",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_ADMIN_TOKEN",
      "ConfigDirectory": "configs",
      "BackupDirectory": "config-backups",
      "AllowConfigWrite": true,
      "AllowRemoteClientManagement": true,
      "AllowAnonymousLoopback": false
    }
  }
}
```

AirTunnel 独立端使用 ControlPlaneAuth 管理会话访问管理 API。首次启动后先访问：

```text
https://air-tunnel.example.com:18643/_airtunnel/control-plane
```

完成 Bootstrap 初始化并登录后，同源浏览器请求会通过会话 Cookie 访问 `/_airtunnel/admin`。脚本调用可以使用登录接口返回的 Ticket，并通过 `X-Air-ControlPlane-Ticket` 请求头携带。

ReverseTunnel 模块的默认管理授权器仍兼容旧 Token 方式。只有未替换 `IReverseTunnelManagementAuthorizer` 的宿主才需要配置：

```bash
export AIR_TUNNEL_ADMIN_TOKEN='<server-management-token>'
```

兼容 Token 请求示例：

```bash
curl -k \
  -H "X-AirTunnel-Admin-Token: $AIR_TUNNEL_ADMIN_TOKEN" \
  https://air-tunnel.example.com:18643/_airtunnel/admin/overview
```

常用接口：

| 接口 | 用途 |
| --- | --- |
| `GET /_airtunnel/admin` | 内置管理页面 |
| `GET /_airtunnel/admin/overview` | Server 状态、控制端口、TCP/UDP listener、在线 Agent、ServiceKey、session 和指标 |
| `GET /_airtunnel/admin/config` | 读取当前运行期配置与拆分配置文件路径 |
| `POST /_airtunnel/admin/config/validate` | 校验候选配置，不写入磁盘 |
| `PUT /_airtunnel/admin/config` | 保存配置、写入备份并热更新运行期快照 |
| `GET /_airtunnel/admin/config/backups` | 查看配置备份目录 |
| `GET /_airtunnel/admin/sessions` | 查看在线 Client/session |
| `POST /_airtunnel/admin/sessions/{sessionId}/disconnect` | 踢掉指定 session |
| `POST /_airtunnel/admin/certificates/revoke` | 按 issuer thumbprint 与 serial number 吊销 Agent 证书 |

当前支持热更新的配置：

| 配置 | 生效方式 |
| --- | --- |
| `AgentAuthorizations` | 更新内存授权表，新 Agent Hello 立即按新白名单判断 |
| `MaxConcurrentStreams` | 已有 session 的新 HTTP 请求按新值判断；正在处理的请求不被强制中断 |
| 请求体、响应体、frame payload 限制 | 新请求和后续数据帧读取运行期快照 |
| `Diagnostics.Enabled` / `SnapshotCollectionIntervalSeconds` | 诊断 API 与采集周期按运行期配置判断 |
| TCP listener 新增、删除、修改 | Server reconcile listener，启动或停止对应 TCP 端口 |
| UDP listener 新增、删除、修改 | Server reconcile listener，启动或停止对应 UDP 端口 |

独立端启动时会自动加载以下拆分配置文件。文件缺失时仍使用 `appsettings.json` 中的 `ReverseTunnelSettings`：

```text
configs/airtunnel.server.json
configs/airtunnel.listeners.json
configs/airtunnel.authorizations.json
configs/airtunnel.limits.json
configs/airtunnel.diagnostics.json
configs/airtunnel.management.json
```

管理端保存配置前会把当前拆分文件复制到 `config-backups/<UTC时间>/`。配置文件只保存路径、端口、授权和策略，不保存 CA 密码、私钥或管理 token。

## Agent 配置

Agent 最小配置结构：

```json
{
  "Urls": "http://127.0.0.1:18543",
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "EnableTunnelProxy": false,
    "AgentId": "branch-agent",
    "ServerAddress": "https://air-tunnel.example.com:18443",
    "AgentConnectionCount": 4,
    "HeartbeatIntervalSeconds": 15,
    "AgentCertificate": {
      "CertificatePath": "/opt/airtunnel/agent/branch-agent/agent.crt",
      "PrivateKeyPath": "/opt/airtunnel/agent/branch-agent/agent.key",
      "TrustedIssuerPath": "/opt/airtunnel/agent/branch-agent/ca.crt",
      "PrivateKeySizeBits": 2048
    },
    "LocalServices": [
      {
        "ServiceName": "Branch Gateway",
        "PublicPath": "/branch",
        "RewritePath": "/tests",
        "Protocol": "http",
        "Host": "127.0.0.1",
        "Port": 5100
      }
    ],
    "Diagnostics": {
      "Enabled": false,
      "PathPrefix": "/_airtunnel/diagnostics",
      "SnapshotPeriodSeconds": 5
    }
  }
}
```

关键配置：

| 配置 | 说明 |
| --- | --- |
| `TunnelMode` | Agent 端配置为 `Agent` |
| `EnableTunnelProxy` | Agent 端配置为 `false` |
| `AgentId` | Agent 稳定身份，必须与 Server `AgentAuthorizations[].AgentId` 一致 |
| `ServerAddress` | Agent 主动连接的 Server 地址 |
| `AgentConnectionCount` | 单 Agent 进程建立的物理隧道连接数，范围 1-64 |
| `AgentCertificate.CertificatePath` | Server 签发后的 Agent 证书保存路径 |
| `AgentCertificate.PrivateKeyPath` | Agent 本地私钥路径；不存在时会在首次 Enrollment 时生成 |
| `AgentCertificate.TrustedIssuerPath` | 可信 CA 证书路径，用于校验 Server HTTPS 证书 |
| `LocalServices` | HTTP 公开路径到 Agent 本地 HTTP 服务的映射；使用 `Protocol`、`Host`、`Port` 指定目标，`RewritePath` 可选；内部 ServiceKey 自动生成 |

Agent 目录必须在启动前存在。私钥、证书和 CA 文件由当前实现写入或读取本地挂载路径，不写入配置中心。

## 首次 Enrollment

Agent 只需挂载与 Server CA PFX 对应的公钥证书 `TrustedIssuerPath`。首次启动时，Agent 生成随机 GUID 并用 CA 公钥 RSA-OAEP-SHA256 加密；Server 用 CA 私钥解密并在 `ReplayWindowSeconds`（默认 300 秒）内原子拒绝重复 GUID。无需生成、分发或挂载 `enrollment.token`。

## HTTP Gateway 接入

入口 Gateway 不承载 Server 时，需要配置远程 AirTunnel Server；HTTP 路由不再在 Gateway 或 Ocelot 中重复声明：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "None",
    "EnableTunnelProxy": true,
    "Gateway": {
      "RemoteServerAddress": "https://air-tunnel.example.com:18443",
      "InvokePathPrefix": "/_airtunnel/gateway",
      "AccessTokenEnvironmentVariable": "AIR_TUNNEL_GATEWAY_TOKEN"
    }
  }
}
```

Gateway 自有中间件按 Agent 配置中的 `PublicPath` 匹配请求，并剥离公开前缀后转发。例如：

```json
{
  "PublicPath": "/branch",
  "RewritePath": "/tests",
  "Protocol": "http",
  "Host": "127.0.0.1",
  "Port": 5100
}
```

请求 `GET /branch/orders` 后，Agent 实际收到 `/tests/orders`。`PublicPath` 末尾也可使用 `{path}`，例如 `/api/orders/{path}`。旧的 `Routes`、`VirtualHost` 和手工 `ServiceKey` 配置不会被读取。

入口 Gateway 与 AirTunnel Server 使用同一个访问令牌环境变量时，二者的 `AIR_TUNNEL_GATEWAY_TOKEN` 值必须一致。

## TCP / UDP listener

TCP/UDP 映射统一配置在 Agent。Server 只需要启用对应协议；保存 Agent 目标配置后，Server 自动从 `LocalServices` 派生对外 listener。Agent 本地 `Port` 与 Server 对外 `ServerListenPort` 是独立端口。

```json
{
  "ReverseTunnelSettings": {
    "Tcp": {
      "Enabled": true,
      "LocalServices": [
        {
          "ServiceName": "Redis",
          "Host": "127.0.0.1",
          "Port": 6379,
          "ServerListenHost": "0.0.0.0",
          "ServerListenPort": 16379
        }
      ]
    }
  }
}
```

UDP listener 与 TCP 类似，Server 绑定入口 UDP 端口，Agent 绑定本地 UDP 目标。UDP 不保证送达、顺序或重传，应用协议需要自行处理 UDP 语义。

## 启动方式

前台启动：

```powershell
cd .\.deploy\airtunnel
.\air.cloud.airtunnel.exe --urls "https://0.0.0.0:18443"
```

使用 `dotnet` 启动：

```powershell
dotnet .\air.cloud.airtunnel.dll --urls "https://0.0.0.0:18443"
```

Linux systemd 示例：

```ini
[Unit]
Description=AirTunnel Server
After=network-online.target
Wants=network-online.target

[Service]
WorkingDirectory=/opt/airtunnel/app
ExecStart=/usr/bin/dotnet /opt/airtunnel/app/air.cloud.airtunnel.dll
Environment=AIR_TUNNEL_CA_PASSWORD=<ca-pfx-password>
Environment=AIR_TUNNEL_GATEWAY_TOKEN=<gateway-to-server-token>
Restart=always
RestartSec=5
User=airtunnel

[Install]
WantedBy=multi-user.target
```

Agent 可以使用同样方式部署为 systemd 服务，只需要替换工作目录、配置文件和环境变量。

当前仓库未提供 AirTunnel 官方 Dockerfile 或镜像。如果容器化部署，应至少挂载：

- `appsettings.json`
- Server HTTPS 证书
- Server CA PFX
- Agent 证书目录
- 运行日志目录

容器端口需要按 `Urls`、Agent 配置中派生出的 `ServerListenPort` 以及 UDP 对外端口显式映射。

## 端口与防火墙

| 端口 | 协议 | 方向 | 说明 |
| --- | --- | --- | --- |
| `18443` | TCP | Gateway / Agent -> Server | Server HTTPS、gRPC 双向流、Gateway 远程调用入口 |
| `18643` | TCP | 管理人员 -> Server | Server 管理端口；AirTunnel 独立端使用 ControlPlaneAuth 会话，默认模块授权器兼容 `X-AirTunnel-Admin-Token` |
| TCP listener 端口 | TCP | 外部客户端 -> Server | 例如 `16379`、`19101` |
| UDP listener 端口 | UDP | 外部客户端 -> Server | 例如 `1053`、`19102` |
| Agent 本地服务端口 | TCP/UDP | Agent -> 本地服务 | 只需要 Agent 所在网络内部可达 |

Server 不需要主动访问 Agent 所在网络。不要为了 AirTunnel 打开从 Server 到 Agent 内网的入站规则。

## 部署验收

### 1. 检查 Server 监听

Windows：

```powershell
netstat -ano | findstr ":18443"
```

Linux：

```bash
ss -lntp | grep ':18443'
```

### 2. 检查诊断 API

诊断开启时：

```bash
curl -k https://air-tunnel.example.com:18443/_airtunnel/diagnostics/snapshot
curl -k https://air-tunnel.example.com:18443/_airtunnel/diagnostics/sessions
```

诊断关闭时应返回 `404`。

### 3. 检查 Agent 会话

在 `sessions` 结果中确认：

- `AgentId` 与配置一致。
- `ServiceKeys` 包含预期服务。
- `LastHeartbeatUtc` 持续刷新。
- 多连接部署时，会话数量符合 `AgentConnectionCount`。

### 4. 检查 HTTP 穿透

从入口 Gateway 调用配置了 `ReverseTunnelDelegatingHandler` 的路由。后端服务应收到请求，且入口 Gateway 不应访问虚拟下游主机。

### 5. 检查 TCP / UDP

TCP：

```bash
nc -vz air-tunnel.example.com 16379
```

UDP 需要使用业务协议或专用 UDP 测试服务确认响应。只看到端口开放不代表 UDP 业务可用。

## 常见故障

| 现象 | 优先检查 |
| --- | --- |
| Agent 无法连接 Server | `ServerAddress`、Server 端口、防火墙、Server HTTPS 证书、Agent `TrustedIssuerPath` |
| Server 日志提示 CA 密码缺失 | `AIR_TUNNEL_CA_PASSWORD` 是否存在于 Server 进程环境 |
| Enrollment 被拒绝 | Agent `TrustedIssuerPath` 是否与 Server CA PFX 对应、Server 是否启用 `Enrollment.Enabled`、加密 GUID 是否在 replay 窗口内重复 |
| Agent 声明 ServiceKey 后仍不可用 | Server `AgentAuthorizations` 是否允许该 AgentId 声明该 ServiceKey |
| Gateway 返回 401 | Gateway 与 Server 的 `AIR_TUNNEL_GATEWAY_TOKEN` 是否一致 |
| Gateway 返回 503 | Server 是否存在可处理该 ServiceKey 的 Agent 会话 |
| Gateway 返回 504 | 后端服务响应慢、Agent 到本地服务不可达、`TimeoutSeconds` 过短 |
| TCP 连接建立后无响应 | Agent 本地 `Tcp.LocalServices` 目标不可达，或 `ServiceKey` 不匹配 |
| UDP 无响应 | UDP 业务协议无返回、`MaxDatagramBytes` 过小、flow 超时过短、防火墙未放行 UDP |

## 升级与回滚

升级前保留：

- 当前程序目录备份
- `appsettings.json`
- Server CA PFX、Server HTTPS 证书
- Agent 证书、私钥、可信 CA
- 运行日志

升级步骤：

1. 发布新版本到新目录。
2. 复制或挂载原配置和证书目录。
3. 在临时端口启动新版本，确认配置可以加载。
4. 停止旧进程。
5. 切换服务工作目录或替换程序目录。
6. 启动新进程。
7. 验证 Server 监听、Agent 会话、Gateway HTTP 穿透和 TCP/UDP listener。

回滚时恢复旧程序目录并保留同一套证书和配置。不要回滚 CA 或 Agent 私钥，除非明确需要恢复到旧证书体系。
