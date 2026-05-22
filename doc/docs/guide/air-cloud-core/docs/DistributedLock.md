### 分布式锁标准（IDistributedLockStandard）

命名空间: `Air.Cloud.Core.Standard.DistributedLock`

概述：分布式锁标准定义了在多实例/多线程环境下的互斥执行能力，提供同步与异步两类 API，并支持等待重试、锁定时长与重试步长等参数配置。实现需满足单例生命周期（`ISingleton`）。

---

### 接口约定

IDistributedLockStandard 继承自 `IStandard, ISingleton`，包含 4 个重载：

| 方法 | 说明 | 关键参数 |
|---|---|---|
| `bool TryExecuteWithLock(string key, Action action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)` | 同步执行，传入 Action | `key`、`WaitMilliseconds`、`StepWaitMilliseconds`、`LockMilliseconds` |
| `bool TryExecuteWithLock(string key, LockAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)` | 同步执行，传入 LockAction 结构 | 同上 + `LockAction`（Success/Fail/Waiting） |
| `Task<bool> TryExecuteWithLockAsync(string key, Func<Task> action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)` | `异步执行，传入 Func<Task>` | 同上 |
| `Task<bool> TryExecuteWithLockAsync(string key, LockAsyncAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000)` | 异步执行，传入 LockAsyncAction 结构 | 同上 + `LockAsyncAction`（Success/Fail/Waiting） |

参数释义：
- key：锁键，字符串标识，建议全局唯一并与业务维度相关（如资源 ID、租户 ID、业务类型）。
- WaitMilliseconds：等待获取锁的最大时长（类型为 TimeSpan）。
- LockMilliseconds：锁持有时长，默认 30000 毫秒（30 秒），用于防止死锁或进程崩溃后无限持锁。
- StepWaitMilliseconds：重试步长，默认 200 毫秒；在等待期内以该步长循环尝试获取锁。

返回值：
- `true` 表示成功获取锁并已执行成功分支；
- `false` 表示在等待期内未成功获取锁（或根据实现策略失败）。

---

### LockAction 与 LockAsyncAction

结构体用于细化不同阶段的回调：

| 结构 | 成员 | 类型 | 说明 |
|---|---|---|---|
| LockAction | Success | `Action` | 成功获取锁时执行 |
| LockAction | Fail | `Action` | 获取锁失败（等待期结束）时执行 |
| LockAction | Waiting | `Action` | 等待期间（每次重试前/后）执行 |
| LockAsyncAction | Success | `Func<Task>` | 成功获取锁时执行（异步） |
| LockAsyncAction | Fail | `Action` | 获取锁失败时执行 |
| LockAsyncAction | Waiting | `Action` | 等待期间执行 |

说明：`Waiting` 的触发时机与频率取决于具体实现（通常在每次重试间隔前后触发一次）。

---

### 行为语义与边界

- 互斥范围：以 `key` 作为互斥粒度，同一 `key` 在同一时间仅允许一个持有者进入成功分支。
- 等待与重试：在 `WaitMilliseconds` 时窗内，以 `StepWaitMilliseconds` 为步长重试加锁。
- 锁持有时长：`LockMilliseconds` 定义最大持有时间，超过将被实现视为超时释放（或可被续约，视实现而定）。
- 返回/回调顺序：成功时先获得锁再执行 `Success`/`action`；失败时执行 `Fail`（若提供），返回 `false`。
- 线程安全：实现应保证并发安全与跨进程可见性（通常基于分布式存储/中间件实现）。
- 生命周期：实现需为单例（`ISingleton`），保证全局一致的锁语义。

---

### 使用示例（编程式）

同步（Action）：

```csharp
var ok = await Task.Run(() =>
    DistributedLock.TryExecuteWithLock(
        key: $"order:close:{orderId}",
        action: () => CloseOrder(orderId),
        WaitMilliseconds: TimeSpan.FromSeconds(3),
        StepWaitMilliseconds: 200,
        LockMilliseconds: 30_000
    )
);
```

同步（LockAction）：

```csharp
var actions = new LockAction
{
    Success = () => CloseOrder(orderId),
    Fail = () => _logger.Warn($"Acquire lock failed: {orderId}"),
    Waiting = () => _metrics.Inc("lock.waiting")
};

var ok = DistributedLock.TryExecuteWithLock(
    key: $"order:close:{orderId}",
    action: actions,
    WaitMilliseconds: TimeSpan.FromSeconds(5)
);
```

异步（`Func<Task>`）：

```csharp
var ok = await DistributedLock.TryExecuteWithLockAsync(
    key: $"billing:run:{batchId}",
    action: async () => await RunBillingAsync(batchId),
    WaitMilliseconds: TimeSpan.FromSeconds(10),
    StepWaitMilliseconds: 300,
    LockMilliseconds: 60_000
);
```

异步（LockAsyncAction）：

```csharp
var asyncActions = new LockAsyncAction
{
    Success = async () => await RunBillingAsync(batchId),
    Fail = () => _logger.Error($"Lock failed for {batchId}"),
    Waiting = () => Progress.Report("waiting...")
};

var ok = await DistributedLock.TryExecuteWithLockAsync(
    key: $"billing:run:{batchId}",
    action: asyncActions,
    WaitMilliseconds: TimeSpan.FromSeconds(10)
);
```

注：以上示例中的 `DistributedLock` 获取方式取决于你的依赖注入方式（示例：`IDistributedLockStandard` 从 IoC 容器解析）。

---

### 使用示例（AOP 特性）

基于拦截器/切面，可用 `DistributedLock` 特性在控制器/方法上声明锁：

```csharp
[HttpGet("lock")]
[AllowAnonymous]
[DistributedLock(3000, FailMessage = "拿锁失败", LockKey = "Lock")]
public bool TryLockSample()
{
    // 在锁内执行
    Thread.Sleep(10000);
    return true;
}

[HttpPost("lock")]
[AllowAnonymous]
[DistributedLock(3000, FailMessage = "拿锁失败")]
public bool TryLockWithBody(object key)
{
    // 在锁内执行
    Thread.Sleep(10000);
    return true;
}
```

说明：
- 构造参数 `3000` 通常表示等待时长（毫秒）；`FailMessage` 为获取锁失败时的提示；`LockKey` 可自定义锁键（如未提供可能由框架根据路由/参数推导）。
- 具体特性参数与推导策略以实际特性定义为准（本页示例来自现有代码片段）。

---

### 接入与注册

当前文档未包含框架内置的默认实现信息。通常建议：

- 如果你的项目引入了 `Air.Cloud.Modules.RedisCache` 模组：该模组内已提供分布式锁标准的实现并自动注入到容器，你可以直接从 IoC 解析 `IDistributedLockStandard` 或通过切面特性使用，无需额外注册。

- 在 IoC 容器中注册你的分布式锁实现（例如基于 Redis/数据库/Consul/ZooKeeper 等）：

```csharp
services.AddSingleton<IDistributedLockStandard, MyRedisDistributedLock>();
```

- 若你的项目使用框架的 AOP/特性方式，请确保对应的特性与拦截器已启用且能从容器解析 `IDistributedLockStandard`。

---

### 最佳实践与注意事项

- 锁键命名：包含业务上下文（资源类型 + 资源 ID + 动作），避免冲突；如 `order:close:{orderId}`。
- 时长设置：`LockMilliseconds` 不宜过短（导致任务未完成即释放），也不宜过长（导致阻塞积压）；根据任务预期时长评估，必要时在实现层支持续约。
- 重试步长：`StepWaitMilliseconds` 过小会增加存储压力，过大则增加等待延迟；根据并发量与存储能力做权衡。
- 幂等性：加锁能降低并发冲突，但不替代幂等性；特别是失败重试场景仍需设计幂等。
- 失败处理：善用 `Fail` 回调或返回值进行补偿与告警；避免静默失败。
- 可观测性：建议在实现中埋点（获取成功率、平均等待、冲突率），便于容量规划与性能优化。

---

### 相关文档

- 标准与注入：`../guide/inject.md`
- 应用切面：若有对应拦截器/特性文档（如 `Modules/AppAspect`），可参阅其使用方式与参数说明。
