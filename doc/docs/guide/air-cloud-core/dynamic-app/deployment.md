# AirForge 部署模式

AirForge 支持 Process 模式和已有容器接管。两种模式都通过 DynamicApp Manager API 进入状态表，并由 Gateway 统一转发业务流量。

## Process 模式

`Process` 模式用于在一个 `air.forge` 容器内启动多个业务子进程：

```text
air.forge
  ├─ air.cloud.airforge.dll
  ├─ mods/auth/air.cloud.auth.entry.dll
  ├─ mods/account/air.cloud.account.entry.dll
  └─ mods/system/air.cloud.system.entry.dll
```

`ProcessDynamicAppModRunner` 支持两类入口：

- `Artifact.File` 指向 `.exe` 或 Linux 可执行文件时，直接启动该文件。
- `Artifact.File` 指向 `.dll` 时，自动使用 `dotnet <dll> <arguments>` 启动。

发布脚本：

```powershell
.\demo\airForge\publish-outofprocess.ps1 -Clean
```

默认输出目录：

```text
X:\air.cloud.tmp\airforge-outofprocess
```

输出结构：

- `/app/air.cloud.airforge.dll`：AirForge Host 主进程。
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
  -> air.forge:8080
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
  air-forge:
    container_name: air.forge
    image: air-cloud-base-amd64:10.0.0.0
    environment:
      ENTRY_DLL: air.cloud.airforge.dll
    ports:
      - "5110:8080"
    volumes:
      - /opt/docker/air/airforge:/app
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
- 启动健康检查会在 `StartupTimeoutSeconds` 窗口内重复探测，避免进程刚启动但端口尚未监听时误判失败。
- Host 重启后的恢复流程允许短暂探活失败；只要当前 Runner 仍持有该子进程且后续探活成功，状态会自动从 `Failed/Unhealthy` 修正为 `Active/Healthy`。若进程已经退出，或健康探测持续失败，才会保持失败并进入自愈重试。

当同一包版本存在多个部署实例时，停止请求必须使用包含 `deploymentId` 的运行实例键。已有容器编排器优先使用目标状态中登记的 `InstanceId`，命令编排器则把 `{deploymentId}` 注入 `StopArgumentsTemplate`；这样更新、排空和失败回滚不会误停同版本的其他副本。容器名称模板也支持 `{deploymentId}`。

命令编排器的部署 `ArgumentsTemplate` 同样支持 `{deploymentId}`，生成的运行目标会持久化该标识。若外部平台按部署实例命名或隔离副本，应在包装命令中使用这个占位符。

When a package version has multiple deployments, stop requests must use a runtime key containing `deploymentId`. The existing-container orchestrator prefers the `InstanceId` recorded on that target, while the command orchestrator injects `{deploymentId}` into `StopArgumentsTemplate`; update, drain, and failure rollback therefore do not stop sibling replicas. Container-name templates also support `{deploymentId}`.

The command orchestrator's deployment `ArgumentsTemplate` also supports `{deploymentId}`, and the resulting runtime target persists that identifier. Use it in wrapper commands when the external platform names or isolates replicas by deployment.
## Cluster Replicas and Gray Release

Do not reuse the source package directory for another running instance. The same package version may be used by a gray release, and each deployment must have an isolated copy:

```text
mods/security-platform/auth/101-process-current/
  package.zip
  content/

deployments/security-platform/auth/101-process-current/blue/
  package.zip
  content/

deployments/security-platform/auth/101-process-current/gray/
  package.zip
  content/
```

Create a deployment copy from an existing package:

```http
POST /dynamicapp-manager/deployments/create
X-DynamicApp-Manager-Ticket: <ticket>
Content-Type: application/json
```

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth",
  "version": "101-process-current",
  "deploymentId": "gray",
  "port": 5111,
  "endpoint": "http://127.0.0.1:5111",
  "start": false
}
```

`start=false` only creates and registers the isolated deployment. `start=true` starts the copied Process artifact, probes its health endpoint, and activates the package version after startup succeeds. The source package is never overwritten by this operation.

Before copying files, Manager validates the request against registered runtime targets:

- The same `DynamicApp/Mod/Version/DeploymentId` cannot be created twice.
- The same listener `scheme + host + port` cannot be used by another target. URL paths are ignored for this check because they do not create separate process listeners.
- Loopback and wildcard listener addresses are also preflight-bound on the local host, so a port occupied by an unrelated process is rejected before package copying.
- Conflicts return HTTP `400` with `errorCode=dynamicapp.invalid_operation`, and no deployment directory is created.
- Within one DynamicApp process, validation, directory copy, and state registration are serialized; startup and health probing run after the creation gate is released.
- When multiple Host processes share one package root, the root must be writable and mounted at the same path; DynamicApp also uses `.locks/deployment-create.lock` to coordinate cross-process creation.
- The package-store directory check remains as a second protection for stale state or concurrent requests.

The runtime state key is `DynamicApp/Mod/Version/DeploymentId`. This key is also used for Process logs and persisted self-healing retry counters, so a failed replica does not contaminate another replica of the same package version. Old requests without `DeploymentId` continue to address the `default` deployment.

Manager lifecycle requests may include the same `deploymentId` field:

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth",
  "version": "101-process-current",
  "deploymentId": "gray"
}
```

`POST /dynamicapp-manager/start`, `/stop`, `/restart`, `/health`, and `/logs` use this field to operate on one isolated instance. Omitting it selects the legacy `default` instance. `GET /dynamicapp-manager/deployments` lists non-default instances, while `POST /dynamicapp-manager/deployments/create` creates a new deployment directory from the immutable package inventory.

The control-panel application response keeps one package-version entry and exposes all of its runtime replicas in `deployments`. Use `GET /dynamicapp-manager/target/{dynamicAppId}/{modId}/{version}/{deploymentId}` when a detail view must address a specific replica; the three-segment route remains available for the `default` deployment.

## Replica lifecycle and storage governance

DynamicApp serializes lifecycle operations for one runtime key:

```text
DynamicApp/Mod/Version/DeploymentId
```

`start`, `stop`, `restart`, manual `health`, startup restore, self-healing, draining cleanup, and batch deployment use the same in-process operation coordinator. A health probe cannot overwrite the result of a concurrent stop, and self-healing cannot start a second process while a manual restart is still in progress.

State, self-healing retry counters, and archive records are written through an atomic same-directory replacement. If an existing JSON file is malformed, the store starts with an empty snapshot instead of preventing Host startup; the next successful write replaces it with a complete JSON document. The package root should still be mounted on persistent storage in production.

Inspect deployment directories that are present on disk but absent from runtime state:

```http
GET /dynamicapp-manager/deployments/orphans
X-DynamicApp-Manager-Ticket: <ticket>
```

The endpoint is read-only. It intentionally reports orphan directories instead of deleting them automatically, because an operator may need to recover a state file or verify a gray-release artifact first.

Remove one isolated Process deployment after confirmation:

```http
POST /dynamicapp-manager/deployments/remove
X-DynamicApp-Manager-Ticket: <ticket>
Content-Type: application/json
```

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth",
  "version": "101-process-current",
  "deploymentId": "gray"
}
```

The default deployment cannot be removed through this endpoint. ExistingContainer deployments are also rejected because their lifecycle belongs to Docker. For Process deployments, DynamicApp stops the selected child process first, deletes only `deployments/{app}/{mod}/{version}/{deploymentId}`, and then removes the corresponding runtime and self-healing records. If stopping or deletion fails, the state is retained for diagnosis.
