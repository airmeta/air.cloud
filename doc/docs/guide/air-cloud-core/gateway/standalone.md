# AirGateWay部署

`air.gateway` 可以作为独立服务部署。它只负责接收入口 HTTP 请求，通过 Ocelot 路由转发到后端服务；发布物不包含 `Air.Cloud.Modules.ReverseTunnel`，也不会承载 AirTunnel Server、Agent 接入、隧道监听或隧道管理 API。

## Ocelot 配置来源

Gateway 通过 `GatewaySettings:OcelotConfigurationSource` 决定 Ocelot 路由配置从哪里加载：

| 配置值 | 行为 |
| --- | --- |
| `Consul` | 从 Consul 读取服务级配置和公共配置。未配置时使用该模式，兼容已有部署。 |
| `File` | 只从本地 `appsettings.json`、环境变量和命令行参数读取 Ocelot 配置。适合迁移部署、离线部署或本地联调。 |
| `ConsulThenFile` | 先读取 Consul，再叠加本地配置。本地配置优先级更高，适合临时覆盖远程配置。 |

`ConsulThenFile` 使用 .NET 配置叠加规则。本地配置可以覆盖同一路径上的值；数组项按索引合并。如果需要完全脱离远程路由表，应使用 `File` 模式。

## 本地文件模式

本地文件模式下，Ocelot 的 `Routes` 和 `GlobalConfiguration` 直接写入 Gateway 配置文件：

```json
{
  "GatewaySettings": {
    "OcelotConfigurationSource": "File"
  },
  "Routes": [
    {
      "DownstreamPathTemplate": "/v1/identity/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "127.0.0.1",
          "Port": 5108
        }
      ],
      "UpstreamPathTemplate": "/v1/identity/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE", "PATCH" ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://127.0.0.1:5100"
  }
}
```

仓库提供了本地文件模式示例：

```text
demo/airGateway/air.gateway/appsettings.FileMode.example.json
```

## 注意事项

- `File` 模式只表示 Ocelot 路由不从 Consul 读取；是否注册服务到 Consul 由宿主启动和服务注册配置决定。
- 独立部署时应为当前环境重新配置 `AllowCors`、`SkyMirrorShieldSettings`、`TraceLogSettings` 和下游地址。
- 不要把生产密钥、数据库密码或 ES 凭据直接写入示例配置。生产部署应通过环境变量、挂载配置文件或运维配置系统覆盖。
- AirTunnel Server 与 Agent 位于 `demo/airTunnel`，独立运行并管理自己的 HTTP、gRPC、TCP、UDP 和管理端口。
- AirGateway 不再声明 `ReverseTunnelDelegatingHandler` 或 `ReverseTunnelSettings`。如果外部入口需要同时暴露 Gateway 和 AirTunnel，应由负载均衡器或反向代理按已规划的入口域名、端口或路径把流量送到相应独立服务。
- `Air.Cloud.Modules.ReverseTunnel` 仍是可供其他自定义宿主引用的模块，但不属于 AirGateway 产品依赖。

## 控制面管理监听

AirGateway 的控制面页面由 `ControlPlaneAuth` 提供。业务插件可能会对普通请求执行 JWT 或自定义 Authorization 校验，因此控制面路径会在插件宿主之前被识别和放行。

可以在 `config/control-plane-auth.json` 中启用独立管理监听：

```json
{
  "ControlPlaneAuth": {
    "Enabled": true,
    "Management": {
      "Enabled": true,
      "Port": 8081,
      "BindAddress": "0.0.0.0",
      "RestrictToPort": true
    }
  }
}
```

`Port` 是容器内监听端口，宿主端口由 Docker 或主机防火墙映射；`Port=0` 时不增加监听器。`RestrictToPort=true` 时，控制面路径只能从该管理端口访问，业务端口返回 404，不会被插件误报成 401。登录页为 `/_airgateway/control-auth`，登录后的 H5 页面为 `/_airgateway/control-plane`。
