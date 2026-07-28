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

`IDynamicAppStateStore.Activate(...)` 会将目标版本设置为 `Active/Healthy`，并将同一 `dynamicAppId/modId` 下其他版本设置为 `Draining/Draining`，同时记录 `DrainingSinceUtc`。

## 状态存储

运行目标包含以下时间线字段：

- `CreatedAtUtc`
- `UpdatedAtUtc`
- `DrainingSinceUtc`
- `LastStatusMessage`

`InMemoryDynamicAppStateStore` 和 `FileDynamicAppStateStore` 在激活、更新、删除目标时维护这些字段。`IDynamicAppStateStore.RemoveTarget(...)` 用于排空完成后的目标清理。

## 排空清理

`DynamicAppDrainCleanupHostedService` 定时扫描 `Draining` 目标。目标超过 `DynamicAppDrainOptions.DrainTimeout` 后，服务会调用 `IDynamicAppModRunner.StopAsync(...)` 并从状态表移除目标。

配置示例：

```csharp
builder.Services.AddDynamicAppHost(
    configureDrain: options =>
    {
        options.Enabled = true;
        options.ScanInterval = TimeSpan.FromSeconds(15);
        options.DrainTimeout = TimeSpan.FromSeconds(60);
        options.RemoveTargetWhenStopFails = true;
    });
```

## Process 日志与退出监控

`ProcessDynamicAppModRunner` 管理 Process 模式 Mod：

- 启动时订阅 stdout、stderr 和 `Exited` 事件。
- 标准输出和标准错误写入 `IDynamicAppProcessLogSink`。
- 主动 `StopAsync(...)` 会登记为预期退出，并写入 `Stopped/Stopped`。
- 非预期退出会写入 `Failed/Unhealthy`，同时记录退出日志。
- 默认日志 Sink 为 `InMemoryDynamicAppProcessLogSink`，单目标默认保留最近 1000 条。

## Process 自愈

`DynamicAppProcessSelfHealingHostedService` 会定时扫描非 `Stopped`、非 `Draining` 的 Process 目标。目标子进程异常退出、状态进入 `Failed/Unhealthy`，或持久化状态中显示可运行但当前 Host 不再持有活进程时，自愈服务会按 Manifest 历史重新启动该 Mod。

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
- 同一目标受退避和最大重试次数限制，避免坏包无限重启。

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
