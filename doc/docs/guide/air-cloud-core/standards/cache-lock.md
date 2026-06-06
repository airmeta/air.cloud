# 缓存与锁标准

缓存与锁标准用于抽象应用缓存、Redis 能力和分布式锁能力。它们通常用于跨实例共享状态、热点数据缓存和并发控制。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IAppCacheStandard` | 应用缓存统一入口 |
| `IRedisCacheStandard` | Redis 缓存标准入口 |
| `IRedisCacheKeyStandard` | Redis Key 构建与管理标准 |
| `IDistributedLockStandard` | 分布式锁标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.RedisCache` | Redis 缓存、Redis Key、分布式锁实现 |

## 快速接入

业务项目需要先引用 Redis 模块包：

```xml
<PackageReference Include="Air.Cloud.Modules.RedisCache" Version="1.0.2" />
```

Redis 模块启动时会注册缓存与锁标准：

```csharp
services.AddRedisCacheService<RedisCacheDependency>();
services.AddSingleton<IDistributedLockStandard, RedisLockDependency>();
```

配置节点名称为 `RedisSettings`：

```json
{
  "RedisSettings": {
    "ConnectionString": "127.0.0.1:6379,abortConnect=false",
    "UserName": "",
    "Password": "123456"
  }
}
```

没有 ACL 用户时 `UserName` 可以留空；没有密码时 `Password` 可以留空。`ConnectionString` 会传给 `StackExchange.Redis.ConnectionMultiplexer.Connect()`。

## 应用缓存示例

业务代码优先依赖 `IAppCacheStandard`。这样后续即使缓存实现从 Redis 换成其他实现，业务调用点也不需要改。

```csharp
using Air.Cloud.Core.Standard.Cache;

public sealed class UserProfileCache
{
    private readonly IAppCacheStandard _cache;

    public UserProfileCache(IAppCacheStandard cache)
    {
        _cache = cache;
    }

    public void Save(UserProfile profile)
    {
        var key = $"user:profile:{profile.Id}";
        _cache.SetCache(key, profile, TimeSpan.FromMinutes(30));
    }

    public UserProfile Get(string userId)
    {
        return _cache.GetCache<UserProfile>($"user:profile:{userId}");
    }

    public void Remove(string userId)
    {
        _cache.RemoveCache($"user:profile:{userId}");
    }
}

public sealed class UserProfile
{
    public string Id { get; set; }

    public string Name { get; set; }
}
```

简单字符串也可以直接读写：

```csharp
_cache.SetCache("system:maintenance", "off", TimeSpan.FromMinutes(5));

var value = _cache.GetCache("system:maintenance");
```

在暂时不方便通过构造函数注入的框架入口、启动流程或静态辅助代码中，可以通过 `AppRealization` 访问当前标准实现。`AppRealization.Cache` 内部仍会优先从 `AppCore.GetService<IAppCacheStandard>()` 解析 DI 中注册的实现；没有实现时才会落到默认兜底并抛出未实现异常。

```csharp
AppRealization.Cache.SetCache("order:latest", "A0001", TimeSpan.FromMinutes(10));

var latestOrderNo = AppRealization.Cache.GetCache("order:latest");
```

## Redis 扩展示例

只有业务确实需要 Redis 原生数据结构时，才依赖 `IRedisCacheStandard`。

```csharp
using Air.Cloud.Core.Standard.Cache.Redis;

public sealed class OrderStatusCache
{
    private readonly IRedisCacheStandard _redis;

    public OrderStatusCache(IRedisCacheStandard redis)
    {
        _redis = redis;
    }

    public void SaveStatus(string orderId, string status)
    {
        _redis.Hash.Set("order:status", orderId, status);
    }

    public string GetStatus(string orderId)
    {
        return _redis.Hash.Get<string>("order:status", orderId);
    }

    public void ExpireOrderDetail(string orderId)
    {
        _redis.Key.Expire($"order:detail:{orderId}", TimeSpan.FromMinutes(15));
    }
}
```

常用入口：

| 入口 | 适用场景 |
| --- | --- |
| `String` | 简单字符串、序列化对象、批量键值读写 |
| `Hash` | 同一业务对象下多个字段，例如订单状态、用户属性 |
| `List` | 队列、栈、有序列表 |
| `Set` | 去重集合、交集、并集、差集 |
| `SortedSet` | 排行榜、按分值排序、范围查询 |
| `Key` | 删除、存在性判断、过期时间、重命名 |
| `Change(index)` | 切换 Redis database index |
| `GetDatabase()` | 获取底层 `StackExchange.Redis.IDatabase` |

切换数据库索引：

```csharp
var redisDb1 = AppRealization.RedisCache.Change(1);

redisDb1.SetCache("tenant:1001:flag", "enabled", TimeSpan.FromMinutes(5));
```

注意：`Change(index)` 返回新的 `IRedisCacheStandard` 实例，不会修改全局默认实例。

## 分布式锁示例

分布式锁适合保护“同一资源同一时间只能被一个实例处理”的业务，例如订单关闭、库存扣减、定时任务单实例执行、批次结算等。

推荐使用 `LockAction` 或 `LockAsyncAction`，因为它们能显式处理等待、成功和失败。简化重载只传 `Action` / `Func<Task>` 时，默认失败回调为空，业务不容易感知拿锁失败。

同步示例：

```csharp
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DistributedLock;

public sealed class OrderCloseJob
{
    private readonly IDistributedLockStandard _lock;

    public OrderCloseJob(IDistributedLockStandard distributedLock)
    {
        _lock = distributedLock;
    }

    public void Close(string orderId)
    {
        var lockKey = $"lock:order:close:{orderId}";

        _lock.TryExecuteWithLock(
            lockKey,
            new LockAction
            {
                Success = () =>
                {
                    // 这里放只能被一个实例同时执行的业务逻辑。
                    CloseOrder(orderId);
                },
                Waiting = () =>
                {
                    AppRealization.Output.Print(
                        "订单关闭锁",
                        $"正在等待订单关闭锁,订单:{orderId}",
                        AppPrintLevel.Information);
                },
                Fail = ex =>
                {
                    AppRealization.Output.Print(
                        "订单关闭锁",
                        $"获取订单关闭锁失败,订单:{orderId},异常:{ex.Message}",
                        AppPrintLevel.Warn,
                        AdditionalParams: new Dictionary<string, object>
                        {
                            ["orderId"] = orderId,
                            ["lockKey"] = lockKey,
                            ["exception"] = ex
                        });
                }
            },
            WaitMilliseconds: TimeSpan.FromSeconds(3),
            StepWaitMilliseconds: 200,
            LockMilliseconds: 30000);
    }

    private void CloseOrder(string orderId)
    {
        // 业务处理。
    }
}
```

异步示例：

```csharp
public async Task RebuildAsync(string tenantId)
{
    var lockKey = $"lock:tenant:index-rebuild:{tenantId}";

    await _lock.TryExecuteWithLockAsync(
        lockKey,
        new LockAsyncAction
        {
            Success = async () =>
            {
                await RebuildTenantIndexAsync(tenantId);
            },
            Waiting = () =>
            {
                AppRealization.Output.Print(
                    "租户索引重建锁",
                    $"正在等待租户索引重建锁,租户:{tenantId}",
                    AppPrintLevel.Information);
            },
            Fail = ex =>
            {
                AppRealization.Output.Print(
                    "租户索引重建锁",
                    $"获取租户索引重建锁失败,租户:{tenantId},异常:{ex.Message}",
                    AppPrintLevel.Warn,
                    AdditionalParams: new Dictionary<string, object>
                    {
                        ["tenantId"] = tenantId,
                        ["lockKey"] = lockKey,
                        ["exception"] = ex
                    });
            }
        },
        WaitMilliseconds: TimeSpan.FromSeconds(10),
        StepWaitMilliseconds: 500,
        LockMilliseconds: 120000);
}
```

参数含义：

| 参数 | 说明 | 建议 |
| --- | --- | --- |
| `key` | 锁 Key，同一个 Key 互斥 | 包含业务类型和资源 ID，例如 `lock:order:close:{orderId}` |
| `WaitMilliseconds` | 最多等待多久去获取锁 | 用户请求通常设置短一些，后台任务可以更长 |
| `StepWaitMilliseconds` | 每轮重试等待步长 | 不要过小，避免高并发下频繁打 Redis |
| `LockMilliseconds` | 锁租约时长 | 必须覆盖业务最大执行时间，并留出余量 |

当前 Redis 锁实现会为每次加锁生成唯一 token，并只用该 token 释放当前实例持有的锁。实现没有自动续约能力，所以 `LockMilliseconds` 不能短于业务实际执行时间。

## 异常传递规则

`RedisLockDependency` 的 `LockAction` / `LockAsyncAction` 重载会把失败原因传给 `Fail(Exception)`：

| 失败路径 | 传给 `Fail` 的异常 |
| --- | --- |
| Redis database 为空 | `Redis database is null` |
| 等待窗口内一直没有拿到锁 | `Failed to acquire lock, key: {key}` |
| `Success` 业务逻辑抛出异常 | 原始业务异常 |
| Redis 锁系统异常 | 原始系统异常 |

需要注意的是，简化重载：

```csharp
TryExecuteWithLock(string key, Action action, TimeSpan waitTimeout, ...)
TryExecuteWithLockAsync(string key, Func<Task> action, TimeSpan waitTimeout, ...)
```

内部会包装成下面的空回调：

```csharp
new LockAction
{
    Success = action,
    Fail = ex => { },
    Waiting = () => { }
}
```

所以不是实现没有传递异常，而是简化重载没有给调用方暴露失败处理入口。关键业务建议直接使用 `LockAction` / `LockAsyncAction`，在 `Fail` 中记录日志、埋点、返回业务提示或触发补偿。

## 特性方式示例

Web 接口也可以使用 `DistributedLockAttribute` 声明锁。默认锁 Key 会包含控制器全名、方法名和参数序列化结果；指定 `LockKey` 后会优先按指定键生成锁。

```csharp
using Air.Cloud.Core.Standard.DistributedLock.Attributes;

[HttpPost("orders/{orderId}/close")]
[DistributedLock(
    WaitLockMilliseconds: 3000,
    LockKey: "orderId",
    FailMessage: "订单正在处理中,请稍后再试",
    StepWaitMilliseconds: 200,
    LockMilliseconds: 30000)]
public IActionResult CloseOrder(string orderId)
{
    // 在锁内执行。
    return Ok();
}
```

如果 `LockMilliseconds` 小于 `WaitLockMilliseconds`，特性构造函数会把锁定时间修正为 `WaitLockMilliseconds + 1000`，避免锁租约比等待窗口还短。

## 锁常见问题与处理

| 问题 | 常见场景 | 表现 | 处理方式 |
| --- | --- | --- | --- |
| 锁 Key 过粗 | 多个租户、多个订单共用同一个 `lock:order` | 不相关业务互相排队，吞吐下降 | Key 中加入租户、资源 ID、动作，例如 `lock:order:close:{orderId}` |
| 锁 Key 过细 | 每次请求都用随机 Key | 实际没有互斥效果，并发冲突仍然发生 | Key 必须稳定表达同一个共享资源 |
| 等待超时 | 同一资源并发请求过多，或持锁业务执行太慢 | 进入 `Fail` 回调，用户看到“系统繁忙”类提示 | 缩短锁内逻辑；对用户请求快速失败；后台任务可延长等待时间并记录告警 |
| 锁租约太短 | `LockMilliseconds` 小于业务执行时间 | 第一个实例还没执行完，第二个实例重新拿到锁，出现重复执行 | 按 P99 执行时间设置租约并留余量；长任务拆分阶段；必要时实现续约 |
| 锁租约太长 | 进程崩溃、网络中断或业务卡死 | 其他实例必须等到租约过期才能继续 | 设置合理上限；锁内逻辑支持超时取消；把长 IO 移出锁外 |
| 锁内业务异常 | `Success` 中抛出异常 | 当前实现会记录错误、释放锁，然后执行 `Fail` 回调 | 在 `Fail` 中记录上下文；业务侧做补偿；不要依赖锁替代事务 |
| 失败处理为空 | 使用只传 `Action` / `Func<Task>` 的简化重载 | 异常会进入内部空 `Fail`，调用方无感知 | 关键业务使用 `LockAction` / `LockAsyncAction`，在 `Fail` 中打日志、埋点或返回明确结果 |
| Redis 不可用 | Redis 连接失败、网络抖动、实例重启 | 加锁失败或执行 `Fail`，可能伴随 Redis 锁系统异常日志 | 对关键链路配置降级策略；监控 Redis 可用性；业务必须能处理未执行结果 |
| 重试压力过大 | 高并发下 `StepWaitMilliseconds` 设置过小 | Redis 请求增多，等待回调频繁 | 增大步长；对热点资源做排队、限流或合并请求 |
| 非幂等副作用 | 锁超时后重试，或业务执行到一半失败 | 重复扣减、重复发消息、重复生成单据 | 写入唯一业务流水号；数据库加唯一约束；消息消费保持幂等 |
| 多资源加锁死等 | 一个流程同时锁订单、库存、账户等多个资源 | 不同线程按不同顺序等待，形成长时间阻塞 | 固定加锁顺序；尽量只锁一个业务聚合；必要时改为事务/状态机 |

## 推荐处理模式

用户请求链路：

```csharp
_lock.TryExecuteWithLock(
    $"lock:order:pay:{orderId}",
    new LockAction
    {
        Success = () => Pay(orderId),
        Fail = ex => throw new InvalidOperationException("订单正在处理中,请稍后再试", ex)
    },
    WaitMilliseconds: TimeSpan.FromSeconds(2),
    StepWaitMilliseconds: 200,
    LockMilliseconds: 30000);
```

后台任务链路：

```csharp
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

await _lock.TryExecuteWithLockAsync(
    $"lock:job:settlement:{settlementDate:yyyyMMdd}",
    new LockAsyncAction
    {
        Success = async () => await RunSettlementAsync(settlementDate),
        Waiting = () => AppRealization.Output.Print(
            "结算任务锁",
            $"正在等待结算任务锁,日期:{settlementDate:yyyyMMdd}",
            AppPrintLevel.Information),
        Fail = ex =>
        {
            AppRealization.TraceLog.Write(
                new DefaultTraceLogContent(
                    "结算任务锁",
                    $"未获取到结算任务锁,已跳过本轮任务,日期:{settlementDate:yyyyMMdd}",
                    new Dictionary<string, object>
                    {
                        ["settlementDate"] = settlementDate,
                        ["exception"] = ex
                    },
                    DefaultTraceLogContent.EVENT_TAG,
                    DefaultTraceLogContent.ERROR_TAG));
        }
    },
    WaitMilliseconds: TimeSpan.FromSeconds(30),
    StepWaitMilliseconds: 1000,
    LockMilliseconds: 300000);
```

## 排查步骤

1. 确认项目已引用 `Air.Cloud.Modules.RedisCache`，并且 `RedisSettings.ConnectionString` 能连接到 Redis。
2. 确认 DI 中能解析 `IAppCacheStandard`、`IRedisCacheStandard`、`IDistributedLockStandard`。
3. 确认锁 Key 是否符合业务互斥粒度，避免过粗或过细。
4. 查看 `Fail` 回调和框架输出日志，区分“等待超时”“业务异常”“Redis 异常”。
5. 对高频锁记录等待次数、等待耗时、失败次数和业务执行耗时，再调整 `WaitMilliseconds`、`StepWaitMilliseconds`、`LockMilliseconds`。

## 使用建议

- 业务优先使用 `IAppCacheStandard`，只有确实需要 Redis 特性时再使用 `IRedisCacheStandard`。
- 分布式锁应明确锁 Key、过期时间和释放策略，避免业务异常后长期占锁。
- 缓存标准没有默认实现，未引入模块时调用 `AppRealization.Cache` 会抛出未实现异常。
- 锁只解决互斥执行问题，不替代数据库事务、唯一约束、幂等设计和失败补偿。
