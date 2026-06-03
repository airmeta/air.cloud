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

## 使用建议

- 业务优先使用 `IAppCacheStandard`，只有确实需要 Redis 特性时再使用 `IRedisCacheStandard`。
- 分布式锁应明确锁 Key、过期时间和释放策略，避免业务异常后长期占锁。
- 缓存标准没有默认实现，未引入模块时调用 `AppRealization.Cache` 会抛出未实现异常。
