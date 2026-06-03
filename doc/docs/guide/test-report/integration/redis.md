# Redis 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/Redis/RedisRealInstanceIntegrationTests.cs`。

目标是连接真实 Redis 实例，验证 Redis 缓存、原生结构和分布式锁在真实 Redis 上的行为。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `RedisIntegration:RunRedisTests` | `true` |
| `RedisIntegration:ConnectionString` | `192.168.100.156:6379,allowadmin=true` |
| `RedisIntegration:UserName` | 空 |
| `RedisIntegration:Password` | 空 |
| `RedisIntegration:KeyPrefix` | `air-cloud-it` |

## 3. 测试内容与边界

### 缓存读写

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 字符串缓存 | `BuildKey("string")`，随机 `redis-value-{Guid}`，TTL `1min` | 删除后读取 | 写入成功、读取一致、删除后返回 `null`。 |
| 对象缓存 | `RedisPayload { Id = Guid, Name = redis-object }`，TTL `1min` | 对象序列化/反序列化 | 读取对象非空，Id 与 Name 一致。 |

### 原生结构

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| Hash + Key Rename | `field = field`，`value = hash-value`，Key 重命名 | 旧 Key 查询 | 新 Key 可读字段值，旧 Key 不存在。 |
| List | `RightPush(list-value)` | Pop 后读取 | `RightPop` 返回 `list-value`。 |
| Set | `RedisPayload { Id = set-value }` | 对象序列化成员 | `Elements<RedisPayload>` 包含目标 Id。 |
| SortedSet | `RedisPayload { Id = sorted-value }`，`score = 10` | 按 Rank 读取 | 第一项 Id 为 `sorted-value`。 |

### 分布式锁成功路径

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 同步成功 | 空闲 Key，等待 `3s`，步长 `100ms`，锁时长 `3000ms` | 无竞争 | `Success` 执行 1 次，`Fail` 为空。 |
| 异步成功 | 空闲 Key，异步 `Success` 返回完成任务 | 无竞争 | `Success` 执行 1 次，`Fail` 为空。 |
| 成功后释放 | Success 返回后立即用原生 `LockTake` 获取同一 Key | 锁未释放风险 | 原生获取成功，证明模块释放锁。 |
| 并发串行化 | 4 个任务竞争同一 Key，业务执行 `80ms` | 并发重入风险 | 成功 4 次，最大同时持有者为 1。 |

### 分布式锁失败与异常路径

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 锁被占用 | 原生 `LockTake` 持有 `5s`，模块等待 `120ms`，步长 `20ms`，锁时长 `500ms` | 等待窗口内不可获取锁 | `Success` 0 次，`Fail` 非空，错误包含 `Failed to acquire lock`。 |
| Success 抛异常 | `Success` 抛 `success-callback-error` | 业务异常 | `Fail` 收到原始异常实例。 |
| Waiting 抛异常 | 锁被占用且 `Waiting` 抛 `waiting-callback-error` | 等待通知异常 | Waiting 异常不逃逸，最终仍进入失败补偿。 |
| Fail 抛异常 | `Success` 抛异常后 `Fail` 抛 `fail-callback-error` | 补偿异常 | 抛 `AggregateException`，包含业务异常与补偿异常。 |
| 异步 Success 抛异常 | 异步 `Success` 抛 `async-success-callback-error` | 异步业务异常 | `Fail` 收到原始异常。 |
| 异步 Fail 抛异常 | 异步 `Success` 抛异常，`Fail` 再抛异常 | 异步补偿异常 | 抛 `AggregateException`。 |

### 租约与 Token 边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 参数标准化 | `waitTimeout = 10ms`，`stepWaitMilliseconds = 0`，`lockMilliseconds = 0` | 非法时间参数 | 标准化后仍成功执行一次。 |
| 租约过短风险 | 第一个业务执行 `300ms`，锁租约 `100ms` | 业务执行时间大于锁租约 | 第二个持有者可能在第一个结束前拿到锁，用于证明需配置合理租约或续期。 |
| Token 释放保护 | 第一个锁租约 `80ms`，业务内等待 `180ms` 后第二个 token 获取锁 | 旧持有者 finally 释放时可能误删新锁 | 第二个 token 能持有并释放自己的锁，旧 token 不会释放新锁。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`16/16` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "Module=Redis"
```
## 5. 对应目标中间件版本

目标 Redis 实例为 `192.168.100.156:6379`。当前配置未声明 Redis 服务端版本，报告不臆测具体版本。

