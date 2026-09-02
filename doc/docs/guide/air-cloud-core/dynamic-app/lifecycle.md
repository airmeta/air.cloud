# 生命周期与排空

DynamicApp 使用生命周期和健康状态共同决定 Gateway 是否可以把新流量转发到某个 Mod 目标。

## 生命周期状态

```text
Pending -> Starting -> Active -> Draining -> Stopped
                    \-> Failed
```

| 状态 | 含义 |
| --- | --- |
| `Pending` | 已声明但尚未启动 |
| `Starting` | 已发布或正在启动，等待健康检查 |
| `Active` | 当前接收 Gateway 新流量 |
| `Draining` | 不接收新流量，只等待旧请求和后台任务结束 |
| `Stopped` | 已停止，可释放资源 |
| `Failed` | 启动、健康检查、退出或编排失败 |

`IDynamicAppStateStore.Activate(...)` 会将目标版本设置为 `Active/Healthy`，并将同一 `dynamicAppId/modId` 下仍可运行的其他版本设置为 `Draining/Draining`，同时记录 `DrainingSinceUtc`；已经 `Stopped` 或 `Failed` 的目标不会被重新伪装成排空状态。

需要只切换某一个副本时使用 `ActivateInstance(...)`（Manager 对应 `ActivateInstanceAsync(...)`）。该操作只把指定 `DeploymentId` 设为 `Active/Healthy`，同一包版本的其他副本保持原状态；不带实例语义的 `Activate(...)` 仍保留“整包版本激活”的兼容行为。

## 状态存储

运行目标包含以下时间线字段：

- `CreatedAtUtc`
- `UpdatedAtUtc`
- `DrainingSinceUtc`
- `LastStatusMessage`

`InMemoryDynamicAppStateStore` 和 `FileDynamicAppStateStore` 在激活、更新、删除目标时维护这些字段。`IDynamicAppStateStore.RemoveTarget(...)` 用于排空完成后的目标清理。

## 排空清理

`DynamicAppDrainCleanupHostedService` 定时扫描 `Draining` 目标。目标超过 `DynamicAppDrainOptions.DrainTimeout` 后，服务会先停止并确认指定实例已经退出，成功后才从状态表移除该实例；停止失败时保留 `Draining/Unhealthy` 状态，等待下一轮重试，避免产生幽灵端口。

配置示例：

```csharp
builder.Services.AddDynamicAppHost(
    configureDrain: options =>
    {
        options.Enabled = true;
        options.ScanInterval = TimeSpan.FromSeconds(15);
        options.DrainTimeout = TimeSpan.FromSeconds(60);
        options.RemoveTargetWhenStopFails = false;
    });
```

## Process 日志与退出监控

`ProcessDynamicAppModRunner` 管理 Process 模式 Mod：

- 启动时订阅 stdout、stderr 和 `Exited` 事件。
- 标准输出和标准错误写入 `IDynamicAppProcessLogSink`。
- 主动 `StopAsync(...)` 会登记为预期退出，并写入 `Stopped/Stopped`。
- 非预期退出会写入 `Failed/Unhealthy`，同时记录退出日志。
- 默认日志 Sink 为 `InMemoryDynamicAppProcessLogSink`，单目标默认保留最近 1000 条。

### 停止、重启与更新的进程边界

`StartAsync`、`StopAsync`、重启和更新必须以实际操作系统进程为准，而不能只依赖当前 Host 进程内的字典：

- 启动前会读取状态目标中的 `InstanceId` 和 `DeploymentEntryFilePath`，如果发现入口一致且 PID 仍存活，则接管该进程，不再重复启动。
- 停止时即使当前 Host 没有该进程对象，也会按持久化 PID 接管并停止；PID 已退出时按幂等成功处理并发布 `Stopped/Stopped`。
- 停止一旦开始，不会因为浏览器断开或请求取消而留下子进程；先等待优雅退出，超时后使用 `Kill(entireProcessTree: true)`，并确认进程已退出后才写入 `Stopped`。
- 退出事件按进程对象校验，旧进程的延迟回调不会移除同一目标后来启动的新进程。

因此，重启和更新流程必须等待停止结果成功后再启动新版本；如果停止返回失败，不能继续启动，也不能把目标伪造为 `Stopped`，否则旧进程仍可能占用端口并继续输出日志。

启动后的运行时检查、健康探测和激活任一步失败时，控制面会先执行停止和退出确认，再写入 `Failed/Unhealthy`。删除、归档和销毁也必须以真实停止结果为前提；持久化状态为 `Stopped` 只表示上一次控制面观察结果，销毁前仍会再次核对运行态。

所有实例级生命周期操作使用共享包目录下的跨进程锁，同时保留进程内互斥。这样多个 AirForge Host 共享同一包目录时，启动、停止、更新、归档和恢复不会同时改写同一实例。

文件型状态、Manifest、动态路由和归档存储在写入前都会获取与快照文件关联的跨进程锁并重载最新内容，再按目标键追加或更新后原子替换文件。这样不同 Host 的后写入不会用旧内存快照覆盖其他 Host 已登记的实例或路由。

激活、更新、归档和包版本删除另外使用 `DynamicApp/Mod` 家族锁，而不是只锁某一个版本；因此不同版本之间的切换也会串行执行。

Manager 通过 `IDynamicAppControlPlaneTransactionLog` 记录 `Prepared`、`Committed` 和 `Aborted` 事务边界。未配置路径时日志跟随 `PackageStore.RootPath/control-plane-transactions.jsonl`；AirForge 样例显式使用 `data/dynamicapp-control-plane-transactions.jsonl`。Host 启动时会把没有终态的准备记录标记为 `Aborted`，便于识别崩溃中断的发布、激活或回滚操作。

配置示例：

```json
{
  "DynamicApp": {
    "ControlPlaneTransactionLog": {
      "FilePath": "data/dynamicapp-control-plane-transactions.jsonl"
    }
  }
}
```

### 状态转换约束

状态存储会拒绝不符合生命周期的直接写入，避免控制面通过 `update-state` 制造“已停止但健康”或“已排空但健康”的矛盾快照。允许的主要转换为：

```text
Pending  -> Starting | Stopped | Failed
Starting -> Active | Draining | Stopped | Failed
Active   -> Draining | Stopped | Failed
Draining -> Stopped | Failed
Stopped  -> Starting | Failed
Failed   -> Starting | Active | Stopped
```

同一生命周期状态可以刷新健康状态，但健康组合仍受约束：`Stopped` 必须配 `Stopped`，`Draining` 只能配 `Draining` 或停止失败时的 `Unhealthy`，`Failed` 必须配 `Unhealthy`。非法更新会返回失败结果并保留原快照。

### 更新事务与恢复

`POST /dynamicapp-manager/deployments/update` 使用旧实例的端口和端点，不接受通过更新请求修改监听位置。流程顺序为：

1. 校验当前实例、目标包和目标版本。
2. 停止旧实例并确认进程或容器已经退出。
3. 创建新部署副本，启动并完成运行态检查、健康检查和激活。
4. 任一步失败时，保留新实例的失败诊断，并自动重新启动旧实例；旧实例只有重新启动、健康检查和重新激活全部通过才报告“回滚成功”。

如果回滚也失败，接口会同时返回新版本失败原因和旧实例回滚失败原因，此时旧实例保持 `Stopped` 或 `Failed/Unhealthy`，不会伪造为活动状态，便于人工处理端口和进程残留。

## Process 自愈

`DynamicAppProcessSelfHealingHostedService` 会定时扫描非 `Stopped`、非 `Draining` 的 Process 目标。目标子进程异常退出、状态进入 `Failed/Unhealthy`，或持久化状态中显示可运行但当前 Host 不再持有活进程时，自愈服务会按 Manifest 历史重新启动该 Mod。

控制面板的实例详情会从运行状态中的 `DeploymentDirectory` 读取实际运行文件。隔离部署副本扫描 `deployments` 目录；默认挂载的 `/app/mods/...` 实例则直接扫描其登记目录，因此两种实例都能在详情中展开目录树继续查看文件。文件路径以该实例目录为根，接口不会根据浏览器提交的任意路径读取其他目录。

自愈成功路径：

```text
Failed/Unhealthy
  -> Process Runner Start
  -> Health Probe
  -> Activate
  -> Active/Healthy
```

自愈不会重启人工停止的目标：

- `Stopped/Stopped` 表示人工停止或控制面停止，不会自动拉起。
- `Draining/Draining` 表示版本排空，不会自动拉起。
- Manifest 丢失、启动失败或健康探测失败时保持 `Failed/Unhealthy`。

如果 Host 重启恢复期间出现短暂的 `Connection refused`，但子进程随后仍存活并且健康探测恢复，健康监控和自愈服务会先确认该进程仍由当前 Runner 持有，再将目标恢复为 `Active/Healthy`。这样不会出现“进程已经运行、页面仍显示 Failed、再次启动返回 already running”的状态分裂；`Stopped` 和 `Draining` 目标仍不会被自动恢复。
- 同一目标受退避和最大重试次数限制，避免坏包无限重启。

健康监控、自愈、启动恢复和排空清理服务取得实例锁后都会重新读取状态存储，而不是继续使用等待锁前的候选快照。如果人工停止、启动、重启或排空操作在后台服务等待锁期间完成，后台服务会跳过已经失效的本轮操作；因此 `Stopped/Stopped` 不会被旧快照覆盖为 `Active/Healthy`，刚启动的实例也不会被旧的排空快照再次停止。

配置示例：

```csharp
builder.Services.AddDynamicAppHost(
    configureSelfHealing: options =>
    {
        options.Enabled = true;
        options.ScanInterval = TimeSpan.FromSeconds(5);
        options.RestartBackoff = TimeSpan.FromSeconds(5);
        options.MaxRestartAttempts = 5;
        options.AttemptWindow = TimeSpan.FromMinutes(5);
        options.ResetAttemptsAfterHealthy = true;
    });
```

配置文件示例：

```json
{
  "DynamicApp": {
    "SelfHealing": {
      "Enabled": true,
      "ScanIntervalSeconds": 5,
      "RestartBackoffSeconds": 5,
      "MaxRestartAttempts": 5,
      "AttemptWindowSeconds": 300,
      "ResetAttemptsAfterHealthy": true
    }
  }
}
```

日志查询接口：

```http
POST /dynamicapp-manager/logs
```

请求体：

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth",
  "version": "1.0.0"
}
```

## 回滚

`IDynamicAppManager.RollbackAsync(dynamicAppId, modId, ...)` 的回滚策略：

- 找到同一 `dynamicAppId/modId` 下最近更新的健康旧版本。
- 排除当前 `Active/Healthy` 版本。
- 候选版本必须是 `Active` 或 `Draining`，且健康状态为 `Healthy`。
- 找到后调用状态存储的 `Activate(...)`，让旧版本重新成为活动版本。

接口：

```http
POST /dynamicapp-manager/rollback
```

请求体：

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth"
}
```

## Self-Healing Observability And Package Governance

Process self-healing now persists retry-window state through `IDynamicAppSelfHealingStateStore`. The demo entry uses `FileDynamicAppSelfHealingStateStore`, and the recommended file paths are:

```json
{
  "DynamicApp": {
    "SelfHealingStateStore": {
      "FilePath": "data/dynamicapp-self-healing.json",
      "SaveOnChange": true
    },
    "TargetArchiveStore": {
      "FilePath": "data/dynamicapp-archives.json",
      "SaveOnChange": true
    }
  }
}
```

The persisted self-healing state contains retry attempts, retry limits, backoff timing, counting-window reset time, pause state, latest failure stage, and latest failure message.

Operations:

```http
GET /dynamicapp-manager/self-healing
POST /dynamicapp-manager/self-healing/reset
GET /dynamicapp-manager/archived
POST /dynamicapp-manager/archive
POST /dynamicapp-manager/unarchive
```

`archive` is the governance path for bad packages or invalid manifests. It stops and removes the runtime target when present, clears self-healing retry state, writes a durable archive record, and filters that version from `/dynamicapp-manager/applications`. Manifest audit history is still available from `/dynamicapp-manager/manifests`.

## Lifecycle Failure Guarantees

发布、启动、停止、重启、更新和销毁都必须以运行时实例为边界执行。发布在清单或状态写入过程中取消或异常时，会回滚本次写入的 Manifest 记录和目标快照；批量部署后续 Mod 失败时，会按逆序停止本批次已经启动的实例。

All lifecycle operations are scoped to one runtime instance. Cancellation or failure while publishing rolls back the manifest record and target snapshots written by that publish. If a later Mod fails during a batch deployment, previously started instances are stopped in reverse order.

Process 启动观察、恢复和自愈健康检查失败后必须确认子进程退出；停止超时会终止整个进程树，并在状态改为 `Stopped` 前等待退出。`ExistingContainer` 目标由容器编排器停止，不能依赖 Process Runner 的“无进程即成功”结果。包创建、实例删除、版本删除和归档共享 DynamicApp/Mod 家族锁，避免删除与复制并发导致端口、目录或状态不一致。

After a Process startup observation, restore, or self-healing health check fails, the child process is explicitly stopped and its exit is confirmed. A stop timeout terminates the complete process tree and waits for exit before publishing `Stopped`. `ExistingContainer` targets are stopped through the container orchestrator, never through a misleading “no process” success from the Process Runner. Package creation, deployment removal, version removal, and archive operations share a DynamicApp/Mod family lock so concurrent copy and deletion cannot leave ports, directories, or state inconsistent.

健康监控恢复 `Container` 或本地开发目标时只依据其编排器/HTTP 探测结果，不会调用只负责 `Process` 的 Runner；停止状态回写失败也会被作为停止失败返回。请求取消后，已经创建的部署目录清理和已启动实例清理继续执行，避免重试时残留目录或端口占用。

When health monitoring recovers a `Container` or local-development target, it relies on the orchestrator/HTTP probe and never calls the Process-only Runner; a failed `Stopped` state write is also reported as a stop failure. After request cancellation, cleanup of a newly-created deployment directory and already-started runtimes continues so retries do not encounter stale directories or occupied ports.

Health monitoring, self-healing, startup restore, and drain cleanup re-read the state store after acquiring the runtime-instance lock instead of continuing with the candidate snapshot captured before lock acquisition. If a manual stop, start, restart, or drain transition completes while a background service is waiting for the lock, that cycle skips the stale operation. This prevents an old `Active` snapshot from overwriting `Stopped/Stopped`, and prevents a newly started instance from being stopped by an old drain snapshot.
