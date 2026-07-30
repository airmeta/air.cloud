# AirGateWay部署

`air.gateway` 可以作为独立服务部署。它负责接收入口 HTTP 请求，通过 Ocelot 路由转发到后端服务，也可以在启用 AirTunnel 时承担入口或后端 Gateway 角色。

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
src/servers/air.gateway/appsettings.FileMode.example.json
```

## 注意事项

- `File` 模式只表示 Ocelot 路由不从 Consul 读取；是否注册服务到 Consul 由宿主启动和服务注册配置决定。
- 独立部署时应为当前环境重新配置 `AllowCors`、`SkyMirrorShieldSettings`、`TraceLogSettings` 和下游地址。
- 不要把生产密钥、数据库密码或 ES 凭据直接写入示例配置。生产部署应通过环境变量、挂载配置文件或运维配置系统覆盖。
- 使用 AirTunnel 时，入口 Gateway 的 Ocelot 路由仍需要显式声明 `ReverseTunnelDelegatingHandler`；普通 Ocelot 路由不需要声明。
