# Docker 模块

`Air.Cloud.Modules.Docker` 提供 Docker Engine 接入能力，并实现 Core 层的 `IContainerEngineStandard`。业务代码或上层模块可以通过该标准读取容器列表、检查容器状态，并对指定容器执行启动、停止操作。

该模块只负责 Docker Engine 访问，不负责创建容器、拉取镜像、生成 Compose 文件或执行业务健康检查。

## 安装

```powershell
dotnet add package Air.Cloud.Modules.Docker --version 10.0.2
```

## 注册 Docker Engine 容器引擎

```csharp
using Air.Cloud.Modules.Docker.Extensions;

builder.Services.AddDockerEngineContainerService(options =>
{
    options.Enabled = true;
    options.DockerSocketPath = "/var/run/docker.sock";
    options.RequestTimeout = TimeSpan.FromSeconds(30);
    options.StartTimeout = TimeSpan.FromSeconds(60);
    options.StopTimeout = TimeSpan.FromSeconds(60);
    options.StopGracePeriodSeconds = 15;
});
```

注册后，应用可以从 DI 中解析 Core 标准：

```csharp
using Air.Cloud.Core.Standard.Container;

var containerEngine = app.Services.GetRequiredService<IContainerEngineStandard>();
```

## 容器化部署

容器化部署时，需要把宿主机 Docker Socket 显式挂载到应用容器：

```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock
```

挂载 Docker Socket 后，应用具备控制宿主机 Docker 容器的能力。该配置只应出现在受信任的控制面服务中。

## 使用示例

```csharp
using Air.Cloud.Core.Standard.Container;

var containerEngine = app.Services.GetRequiredService<IContainerEngineStandard>();

var containers = await containerEngine.ListAsync(includeStopped: true);
var container = await containerEngine.InspectAsync("air.account");

var start = await containerEngine.StartAsync("air.account");
var stop = await containerEngine.StopAsync("air.account");
```

`ContainerEngineInstance` 返回以下主要字段：

| 字段 | 说明 |
| --- | --- |
| `Id` | 容器短 ID |
| `Name` | 容器名称 |
| `Image` | 镜像名称 |
| `Runtime` | 运行时类型，Docker 模块固定为 `Docker` |
| `State` | Docker 原始状态，例如 `running`、`exited` |
| `Status` | Docker 状态描述 |
| `HealthStatus` | Docker Healthcheck 状态；未配置 Healthcheck 时为空 |
| `IsRunning` | 是否处于运行状态 |
| `IsAvailable` | 是否运行且未被 Docker 标记为 `unhealthy` |

## 配置项

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Enabled` | `false` | 是否启用 Docker Engine Socket 访问 |
| `DockerSocketPath` | `/var/run/docker.sock` | Docker Engine Unix Socket 路径 |
| `RequestTimeout` | `30s` | 列表、检查等查询请求超时 |
| `StartTimeout` | `60s` | 启动容器请求超时 |
| `StopTimeout` | `60s` | 停止容器请求超时 |
| `StopGracePeriodSeconds` | `15` | 停止容器时传递给 Docker 的优雅退出秒数 |

## 注意事项

- `Enabled` 默认关闭。未显式开启时，服务不会访问 Docker Engine。
- `InspectAsync` 只读取 Docker Engine 状态，不访问业务 HTTP 端口。
- `StartAsync` 和 `StopAsync` 只操作调用方传入的容器名称或 ID。
- 当前实现面向 Linux Unix Socket 场景；Windows Docker named pipe 不在该实现范围内。
- 如果同一应用同时注册多个 `IContainerEngineStandard` 实现，需要在应用侧明确最终使用哪个实现。
