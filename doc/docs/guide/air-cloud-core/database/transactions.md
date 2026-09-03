# EF Core 事务边界

`Air.Cloud.EntityFrameWork.Core` 只负责 EF Core 本地事务，不启用 XA、AT、TCC 或其他分布式事务协调器。

Short (en-us): `Air.Cloud.EntityFrameWork.Core` manages local EF Core transactions only. It does not enable XA, AT, TCC, or another distributed transaction coordinator.

## 一个本地事务组

一个事务组必须绑定到同一个 Provider 和同一个 `DbConnection` 实例。多个 DbContext 只有在共享该连接实例时，才可以通过 `UseTransaction` 参与同一个事务。仅仅满足“同类型数据库”或“连接字符串相同”并不足够，因为不同连接不能共享同一个 `DbTransaction`。

因此：

- 同一个物理数据库、同一个连接实例的多个上下文可以共享本地事务。
- 不同物理数据库（例如服务 A 使用 Oracle、服务 B 使用 MySQL）不能由 Core 包组成一个本地事务。
- 主库和从库不能加入同一个写事务；读写边界应由业务代码拆开。
- 跨服务写入使用 Outbox、幂等和补偿流程；只有明确需要时，才在独立基础设施中评估 TCC/AT 等方案。

## 提交流程

`IDbContextPool.BeginTransaction` 开启或接管一个本地事务，`CommitTransaction` 会先按顺序保存池中变更，再提交事务；任一保存失败都会回滚。显式事务提交时使用 `SaveChanges(false)`，只有数据库提交成功后才接受实体状态，避免后续上下文失败导致内存状态与数据库回滚结果不一致。池内多个上下文的 `SaveChanges`/`SaveChangesAsync` 按顺序执行，避免 EF Core `DbContext` 并发重入。

```csharp
var pool = serviceProvider.GetRequiredService<IDbContextPool>();
pool.BeginTransaction();
try
{
    await repository.InsertAsync(entity);
    pool.CommitTransaction();
}
catch
{
    pool.RollbackTransaction();
    throw;
}
```

带有 `[UnitOfWork]` 的 Controller 仍可由现有过滤器编排上述生命周期。`AutoSaveChangesFilter` 只适合兼容旧业务，不应作为后台任务、消息消费或跨服务一致性的提交协议。

## 其他写入方式

- EF Core `ExecuteUpdateAsync` 在当前 `DbContext` 已绑定本地事务时，会随该事务执行；它不会自动加入其他数据库的事务。
- Core 包不再提供 ADO.NET 命令、DataTable、存储过程或 SQL 仓储封装。需要原生 SQL 时，业务代码直接调用 EF Core `DatabaseFacade` 的标准 API，并显式传入参数；该调用在当前 `DbContext` 已绑定本地事务时参与该事务。
- Oracle `OracleBulkCopy` 等 Provider 特有批量 API 留在 Oracle 子包，由子包自行声明连接和事务参与规则。

## `Now` 方法迁移

`InsertNow`、`SaveNow` 等方法保留用于兼容现有调用。新代码应优先使用 `Insert`/`InsertAsync` 只改变跟踪状态，在业务边界统一调用 `SavePoolNowAsync` 或由 `[UnitOfWork]` 提交。这样后台任务或后续线程只有在事务提交成功后才能读取到数据；需要可靠通知时，应将 Outbox 记录与业务数据放入同一本地事务。
