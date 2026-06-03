# Redis 缓存与锁单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/Redis`。

目标是验证 `IRedisCacheStandard`、`IAppCacheStandard`、`IDistributedLockStandard` 的契约边界。单元测试不证明真实 Redis 可用性，真实连接由集成测试验证。

## 2. 测试内容与边界

### 缓存读写边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 字符串缓存闭环 | `key = redis-contract:string`，`value = redis-value`，TTL `1min` | 删除后再次读取 | 写入返回 `true`，读取相等，删除返回 `true`，删除后返回 `null`。 |
| 对象缓存闭环 | `Id = payload-1`，`Name = redis-object` | 对象经序列化/反序列化 | 读取对象非空，Id 和 Name 保持一致。 |
| TTL 过期 | TTL `2s`，等待 `3s` | 超过 TTL 后读取 | 返回空值。 |

### 原生结构边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| Hash | `key = hash`，`field = field`，`value = hash-value` | Key Rename 后读取 | 新 Key 可读取旧字段值。 |
| List | `RightPush("list", "list-value")` | Pop 后队列变化 | `RightPop` 返回 `list-value`。 |
| Set | `SetAdd("set", "set-value")` | 重复性由集合保证 | `SetContains` 返回 `true`。 |
| SortedSet | `score = 10`，`value = sorted-value` | 按最小 Rank 读取 | 第一项为 `sorted-value`。 |
| Key Rename | `hash -> hash-renamed` | 旧 Key 查询 | 旧 Key 不存在，新 Key 存在。 |

### 锁成功路径边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 同步成功 | 空闲 Key，等待 `1s`，步长 `10ms`，锁时长 `1000ms` | 无竞争 | `Success` 执行 1 次，`Fail` 不执行。 |
| 异步成功 | 空闲 Key，异步 `Success` 返回完成任务 | 无竞争 | `Success` 执行 1 次，`Fail` 不执行。 |
| 成功后释放 | 同一 Key 连续执行两次 | 第二次重新获取锁 | 成功次数为 2。 |
| 并发串行化 | 4 个工作项竞争同一 Key，业务耗时 `40ms` | 并发进入风险 | 总成功 4 次，最大同时持有者为 1。 |

### 锁失败与异常边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 锁被占用 | 预先 `HoldLock(key)`，等待 `50ms` | 等待期内无法获取锁 | `Success` 0 次，`Fail` 非空，错误包含 `Failed to acquire lock`。 |
| Success 抛异常 | `Success` 抛 `success-callback-error` | 业务处理异常 | `Fail` 收到原始异常实例。 |
| Waiting 抛异常 | 锁被占用，`Waiting` 抛 `waiting-callback-error` | 等待通知异常 | Waiting 异常被隔离，最终仍进入获取锁失败补偿。 |
| Fail 抛异常 | `Success` 抛异常，`Fail` 再抛 `fail-callback-error` | 补偿失败 | 抛出 `AggregateException`，同时包含业务异常和补偿异常。 |
| 异步 Success 抛异常 | 异步 `Success` 抛 `async-success-callback-error` | 异步业务异常 | `Fail` 收到原始异常。 |
| 异步 Fail 抛异常 | 异步 `Success` 抛异常，`Fail` 再抛异常 | 异步补偿失败 | 抛出 `AggregateException`。 |

### 参数与租约边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 参数标准化 | `waitTimeout = 10ms`，`stepWaitMilliseconds = 0`，`lockMilliseconds = 0` | 非法等待步长与锁时长 | 标准化为安全值后仍执行成功。 |
| 租约风险暴露 | 第一个业务执行 `180ms`，锁租约 `60ms` | 业务耗时大于锁租约 | 第二个持有者可能在第一个回调结束前进入，暴露需要合理配置锁时长或续期机制。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`24/24` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~Air.Cloud.UnitTest.Modules.Redis"
```
## 4. 对应目标中间件版本

单元测试目标是框架契约，不绑定 Redis 服务端版本。

