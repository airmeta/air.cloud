# RedisCache

`Air.Cloud.Modules.RedisCache` 是 Air.Cloud 的 Redis 缓存与分布式锁模块。模块基于 `StackExchange.Redis` 实现缓存标准，并提供 Redis 原生数据结构入口。

## 所属 Standard

| Standard | 模块实现 | 说明 |
| --- | --- | --- |
| `IAppCacheStandard` | `RedisCacheDependency` | 应用缓存统一入口 |
| `IRedisCacheStandard` | `RedisCacheDependency` | Redis 缓存扩展入口 |
| `IDistributedLockStandard` | `RedisLockDependency` | Redis 分布式锁实现 |

## 包名

```text
Air.Cloud.Modules.RedisCache
```

## 加载机制

模块 `Startup` 会自动绑定配置并注册服务：

```csharp
services.AddOptions<RedisSettingsOptions>()
    .BindConfiguration("RedisSettings")
    .ValidateDataAnnotations();

services.AddRedisCacheService<RedisCacheDependency>();
services.AddSingleton<IDistributedLockStandard, RedisLockDependency>();
```

`AddRedisCacheService<T>()` 会注册：

| 接口 | 默认实现 |
| --- | --- |
| `IAppCacheStandard` | `RedisCacheDependency` |
| `IRedisCacheStandard` | `RedisCacheDependency` |

## 配置节点

配置节点名称为：

```text
RedisSettings
```

| 配置项 | 说明 |
| --- | --- |
| `ConnectionString` | Redis 连接字符串，传入 `ConnectionMultiplexer.Connect()` |
| `UserName` | Redis 用户名；没有 ACL 用户时可为空 |
| `Password` | Redis 密码；没有密码时可为空 |

## 配置示例

```json
{
  "RedisSettings": {
    "ConnectionString": "192.168.100.156:6379,abortConnect=false",
    "UserName": "",
    "Password": "123456"
  }
}
```

如果 Redis 使用 ACL 用户，可以配置：

```json
{
  "RedisSettings": {
    "ConnectionString": "192.168.100.156:6379,abortConnect=false",
    "UserName": "default",
    "Password": "123456"
  }
}
```

## 基础缓存

业务优先依赖 `IAppCacheStandard`：

```csharp
public sealed class OrderCacheService
{
    private readonly IAppCacheStandard _cache;

    public OrderCacheService(IAppCacheStandard cache)
    {
        _cache = cache;
    }

    public void Save(OrderDto order)
    {
        _cache.SetCache($"order:{order.Id}", order, TimeSpan.FromMinutes(10));
    }

    public OrderDto Get(string id)
    {
        return _cache.GetCache<OrderDto>($"order:{id}");
    }
}
```

也可以通过 `AppRealization` 使用：

```csharp
AppRealization.Cache.SetCache("order:1", "created", TimeSpan.FromMinutes(5));
var value = AppRealization.Cache.GetCache("order:1");
```

## Redis 扩展入口

需要使用 Redis 数据结构时，依赖 `IRedisCacheStandard`：

```csharp
public sealed class RedisOrderService
{
    private readonly IRedisCacheStandard _redis;

    public RedisOrderService(IRedisCacheStandard redis)
    {
        _redis = redis;
    }

    public void SaveStatus(string id, string status)
    {
        _redis.Hash.Set("order:status", id, status);
    }
}
```

`IRedisCacheStandard` 暴露的入口：

| 入口 | 说明 |
| --- | --- |
| `String` | Redis String 操作 |
| `Hash` | Redis Hash 操作 |
| `List` | Redis List 操作 |
| `Set` | Redis Set 操作 |
| `SortedSet` | Redis Sorted Set 操作 |
| `Key` | Key 删除、存在性判断、过期时间、刷新、重命名等操作 |
| `GetDatabase()` | 获取底层 `StackExchange.Redis.IDatabase` |
| `Change(index)` | 切换到指定 Redis database index |

## 切换数据库索引

默认使用 database `0`。如果需要访问其他 database：

```csharp
var redisDb1 = AppRealization.RedisCache.Change(1);
redisDb1.SetCache("cache:key", "value");
```

注意：`Change(index)` 返回新的 `IRedisCacheStandard` 实例，不会修改全局默认实例。

## 获取原生 IDatabase

当标准接口无法覆盖业务场景时，可以获取原生 Redis 客户端：

```csharp
var database = AppRealization.RedisCache.GetDatabase() as StackExchange.Redis.IDatabase;
var result = database.StringIncrement("order:counter");
```

## 分布式锁

Redis 模块会注册 `IDistributedLockStandard`：

```csharp
public sealed class OrderJob
{
    private readonly IDistributedLockStandard _lock;

    public OrderJob(IDistributedLockStandard distributedLock)
    {
        _lock = distributedLock;
    }

    public void Run()
    {
        _lock.TryExecuteWithLock(
            "lock:order:job",
            () =>
            {
                // 执行需要互斥的业务逻辑。
            },
            TimeSpan.FromSeconds(5),
            StepWaitMilliseconds: 200,
            LockMilliseconds: 30000);
    }
}
```

带回调的写法：

```csharp
_lock.TryExecuteWithLock(
    "lock:order:job",
    new LockAction
    {
        Success = () => { },
        Waiting = () => { },
        Fail = ex => Console.WriteLine(ex.Message)
    },
    TimeSpan.FromSeconds(5));
```

异步版本：

```csharp
await _lock.TryExecuteWithLockAsync(
    "lock:order:job",
    async () =>
    {
        await Task.Delay(100);
    },
    TimeSpan.FromSeconds(5));
```

## 注意事项

- `ConnectionString` 是必需项；模块初始化 Redis 连接时会直接读取该配置。
- 默认连接是静态单例 `ConnectionMultiplexer`，配置热更新不会自动重建 Redis 连接。
- `UserName` 和 `Password` 会传入 `ConfigurationOptions.User` / `Password`，如果 Redis 未开启 ACL 或密码认证，可以留空。
- 分布式锁使用唯一 token 获取和释放锁，只在当前实例持有锁时释放。
- `LockMilliseconds` 必须覆盖业务执行时间，否则锁可能在业务执行中途过期。
- 缓存对象序列化依赖 `AppRealization.JSON`，需要保证项目已注册可用的 JSON 标准。
