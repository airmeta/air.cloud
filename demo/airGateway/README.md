# AirGateway 独立部署

`demo/airGateway` 是 AirGateway 独立产品目录。该目录提供源码构建用 `Dockerfile`、运行编排用 `docker-compose.yml`，以及默认挂载目录示例。

## 目录

```text
demo/airGateway/
  Dockerfile
  docker-compose.yml
  config/
    00-airgateway.json
  plugins/
    .gitkeep
```

## 构建镜像

在仓库根目录执行：

```bash
docker build -f demo/airGateway/Dockerfile -t air-cloud-gateway:latest .
```

`Dockerfile` 会从源码发布 `demo/airGateway/air.gateway/air.gateway.csproj`，最终镜像只包含运行时文件。

## 运行

进入 `demo/airGateway` 后执行：

```bash
docker-compose up -d
```

生产部署使用以下固定宿主机端口：业务 HTTP `18081 -> 8080`，控制面板 `18082 -> 8081`。

AirGateway 不承载 AirTunnel Server、Agent 接入、HTTP/TCP/UDP 穿透或隧道管理页面。需要网络穿透时，请独立部署 `demo/airTunnel`；两者使用不同进程、镜像和端口。

### 管理页面

AirGateway 的控制面登录页为 `/_airgateway/control-auth`，登录后的 H5 页面为 `/_airgateway/control-plane`。启用独立管理监听时，示例地址为：

```text
http://127.0.0.1:18082/_airgateway/control-auth
```

`ControlPlaneAuth:Management` 配置项控制管理面：

- `Enabled`：是否启用管理页面和管理 API。
- `Port`：容器内独立监听端口；为 `0` 时不增加监听器。
- `BindAddress`：独立监听绑定地址。
- `RestrictToPort`：是否只允许通过该端口访问控制面路径。

控制台默认页面为“网关配置”，地址 `/_airgateway/control-plane/gateway-config`。该页面只属于 AirGateway；当前仅提供网关路由维护的界面骨架，路由读取、保存和运行时生效接口将在后续接入。

示例配置使用容器端口 `8081`，Compose 将其映射为宿主端口 `18082`。控制面路径会在业务插件链前被识别并放行，不会被业务 Authorization 插件抢先拦截。

`config/control-plane-auth.json` 必须保留在挂载的 `config` 目录中；如果只挂载部分配置文件覆盖该目录，控制面认证配置会被遮住，导致 `18082` 不监听。

Compose 默认从 `runtime/appsettings.json` 挂载节点运行配置。发布包提供 `runtime/appsettings.json.example`，首次部署时复制并按目标环境修改为 `runtime/appsettings.json`；已有生产环境的 runtime 配置应优先保留。

如果 Compose 曾因目标文件不存在而把 `runtime/appsettings.json` 创建成目录，需要先删除该空目录，再创建文件后启动容器。

## 挂载目录

`docker-compose.yml` 默认挂载以下目录：

| 宿主机目录 | 容器目录 | 作用 |
| --- | --- | --- |
| `./config` | `/app/config` | 节点级 JSON 配置覆盖目录 |
| `./plugins` | `/app/plugins` | 网关插件 DLL 目录 |
| `./logs` | `/app/logs` | 日志目录 |
| `./data` | `/app/data` | 运行期数据目录 |

WebApp 启动时会先加载镜像内 `appsettings.json`，再按文件名升序加载 `/app/config/*.json`。因此可以把环境差异、插件启停、Consul 地址、Ocelot 配置来源放入挂载配置目录中。

`/app/config` 目录只放配置对象文件。Consul 导出、备份、运行期数据不要放在该目录下，因为启动器会加载 `config/*.json`，数组型或非配置型 JSON 会导致应用启动失败。

## 插件配置

`config/00-airgateway.json` 默认声明了 6 个插件：

- `LegacyTraceLog`
- `LegacyUrlProxy`
- `WhiteListRequest`
- `HfAuthorizationAdapter`
- `LegacyAuthorization`
- `RemoteIp`

如果某个部署不需要插件，删除对应配置项，或将其 `Enabled` 改为 `false`。如果 `FailOnLoadError=true`，已启用插件的 DLL 缺失会阻止 Gateway 启动。

HF 旧 Token 适配不再调用 gRPC 授权服务，而是在 Gateway 进程内使用 `LegacyAuthorizationSettings:Jwt` 签发 Token；`LegacyAuthorization` 使用同一配置校验。上线替换旧网关前，必须将旧运行目录的 `jwt_keys.txt` 内容迁移到 `data/legacy-authorization/jwt_keys.txt`，并保留共享 Redis 中的 UKey 状态。`WhiteListRequest` 必须保持在 HF 插件之前，避免 `DQ=HF` 的白名单请求进入旧 Token 校验。

## Consul

默认配置使用：

```json
{
  "ConsulServiceOptions": {
    "ConsulAddress": "http://fcj-consul:8500/",
    "ConfigurationServiceName": "FCJ.GateWay",
    "RegistryServiceName": "air.gateway"
  }
}
```

容器与 Consul 在同一个 Docker 网络时，应优先使用 Consul 容器名或服务名访问，不要直接写宿主机 IP。
