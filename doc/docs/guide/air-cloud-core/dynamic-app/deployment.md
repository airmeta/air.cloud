# 部署模式

DynamicApp 支持 Process 模式和已有容器接管。两种模式都通过 Manager API 进入状态表，并由 Gateway 统一转发业务流量。

## Process 模式

`Process` 模式用于在一个 `air.dynamicapp` 容器内启动多个业务子进程：

```text
air.dynamicapp
  ├─ air.cloud.dynamicapp.entry.dll
  ├─ mods/auth/air.cloud.auth.entry.dll
  ├─ mods/account/air.cloud.account.entry.dll
  └─ mods/system/air.cloud.system.entry.dll
```

`ProcessDynamicAppModRunner` 支持两类入口：

- `Artifact.File` 指向 `.exe` 或 Linux 可执行文件时，直接启动该文件。
- `Artifact.File` 指向 `.dll` 时，自动使用 `dotnet <dll> <arguments>` 启动。

发布脚本：

```powershell
.\demo\api\DynamicApp\publish-outofprocess.ps1 -Clean
```

默认输出目录：

```text
X:\air.cloud.tmp\dynamicapp-outofprocess
```

输出结构：

- `/app/air.cloud.dynamicapp.entry.dll`：DynamicApp Host 主进程。
- `/app/mods/auth/air.cloud.auth.entry.dll`：auth 子进程。
- `/app/mods/account/air.cloud.account.entry.dll`：account 子进程。
- `/app/mods/system/air.cloud.system.entry.dll`：system 子进程。
- `/app/manifests/security-platform.process.101.json`：Process 模式 Manifest。

Process 模式 Manifest 示例：

```json
{
  "executionMode": "Process",
  "artifact": {
    "file": "mods/auth/air.cloud.auth.entry.dll",
    "arguments": "--urls http://127.0.0.1:5106",
    "endpoint": "http://127.0.0.1:5106"
  }
}
```

业务流量走向：

```text
frontend/gateway
  -> air.dynamicapp:8080
  -> /mods/auth/*
  -> 127.0.0.1:5106
  -> auth 子进程
```

如果子进程仍使用 `WebInjectInConsul()`，子进程自己的 `appsettings.json` 仍会决定 Consul 地址、服务名和业务环境配置来源；`--urls` 只负责绑定 Kestrel 监听地址。

## 已有容器接管

`DockerExistingContainerDynamicAppOrchestrator` 对应 `ExecutionMode=ExistingContainer`，用于把已经由 Compose、1Panel 或运维脚本创建好的容器纳入 DynamicApp 控制面。

该编排器不直接访问 Docker Socket。容器列表、检查、启动和停止操作由 Core 的 `IContainerEngineStandard` 提供；如果底层运行时是 Docker，需要同时引入并注册 `Air.Cloud.Modules.Docker`。

Docker 模块负责通过 Docker Engine Unix Socket 调用：

```text
/containers/{name}/start
/containers/{name}/stop
```

它不会创建容器、拉取镜像或拼接 shell 命令。

适用场景：

- 把现有微服务纳入 DynamicApp 状态、健康和启停控制。
- 验证 Manager、Host 和 Gateway 协同链路，但暂不改造业务服务镜像构建流程。
- 运维环境已有稳定 Compose 文件，只希望 DynamicApp 控制启动、停止、激活和转发。

配置示例：

```json
{
  "DynamicApp": {
    "DockerExistingContainerOrchestrator": {
      "Enabled": true,
      "ContainerNameTemplate": "air.{modId}",
      "TargetBaseAddressTemplate": "http://air.{modId}:8080"
    },
    "DockerEngine": {
      "Enabled": true,
      "DockerSocketPath": "/var/run/docker.sock",
      "RequestTimeoutSeconds": 30,
      "StartTimeoutSeconds": 60,
      "StopTimeoutSeconds": 60,
      "StopGracePeriodSeconds": 15
    }
  }
}
```

运行容器时必须显式挂载 Docker Socket：

```yaml
services:
  air-dynamicapp:
    container_name: air.dynamicapp
    image: air-cloud-base-amd64:10.0.0.0
    environment:
      ENTRY_DLL: air.cloud.dynamicapp.entry.dll
    ports:
      - "5110:8080"
    volumes:
      - /opt/docker/air/dynamicapp:/app
      - /var/run/docker.sock:/var/run/docker.sock
    networks:
      - fcj
```

验证示例：

```bash
curl -X POST http://127.0.0.1:5110/dynamicapp-manager/deploy \
  -H 'Content-Type: application/json' \
  -H 'X-DynamicApp-Manager-Ticket: <ticket>' \
  --data-binary @security-platform.101.json

curl http://127.0.0.1:5110/dynamicapp-manager/targets \
  -H 'X-DynamicApp-Manager-Ticket: <ticket>'

curl http://127.0.0.1:5110/mods/system/Health
```

注意事项：

- 该编排器只适合受控内网控制面。
- `ContainerNameTemplate` 必须收敛到受控容器命名空间，避免误停其他容器。
- Docker Socket、请求超时和停止优雅退出秒数属于 Docker 模块配置，不属于 DynamicApp 编排器配置。
- 启动健康检查会在 `StartupTimeoutSeconds` 窗口内重复探测，避免容器刚启动但端口尚未监听时误判失败。
