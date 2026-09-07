# EF Core 事务边界

`Air.Cloud.EntityFrameWork.Core` 只负责 EF Core 本地事务，不启用 XA、AT、TCC 或其他分布式事务协调器。

Short (en-us): `Air.Cloud.EntityFrameWork.Core` manages local EF Core transactions only. It does not enable XA, AT, TCC, or another distributed transaction coordinator.

## 注册与生命周期

调用 `AddDatabaseAccessor` 后，框架会在当前依赖注入作用域内注册 `ILocalTransactionManager`。通常一个 HTTP 请求或一个后台任务作用域对应一个事务管理器实例，不应把它缓存为单例，也不应跨线程共享同一个 `DbContext`。

```csharp
services.AddDatabaseAccessor();
```

业务服务可以直接注入管理器：

```csharp
public sealed class OrderService
{
    private readonly ILocalTransactionManager _transactionManager;
    private readonly IRepository<Order> _orders;

    public OrderService(
        ILocalTransactionManager transactionManager,
        IRepository<Order> orders)
    {
        _transactionManager = transactionManager;
        _orders = orders;
    }
}
```

## 推荐：事务作用域

`BeginScope` 是业务代码的推荐入口。调用 `Complete` 才会完成当前事务作用域；未完成就释放作用域时会自动回滚。

```csharp
public async Task CreateOrderAsync(Order order)
{
    await using var transactionScope = _transactionManager.BeginScope();

    await _orders.InsertAsync(order);

    // 最外层 Complete 会保存池中的变更并提交数据库事务。
    transactionScope.Complete();
}
```

异常可以直接向上抛出，不需要重复调用回滚：

```csharp
public async Task CreateOrderAsync(Order order)
{
    await using var transactionScope = _transactionManager.BeginScope();

    await _orders.InsertAsync(order);
    await ValidateOrderAsync(order); // 抛出异常

    transactionScope.Complete();
    // 未执行 Complete，释放作用域时自动回滚。
}
```

如果事务开始时还没有上下文登记到池中，可以使用 `ensureTransaction: true`：

```csharp
await using var transactionScope =
    _transactionManager.BeginScope(ensureTransaction: true);
```

该参数只确保默认 `MasterDbContextLocator` 上下文。非默认数据库应先通过对应仓储或 `Db.GetDbContext<TDbContextLocator>()` 解析，使其加入当前上下文池。

## 显式 Begin、Commit、Rollback

需要明确控制生命周期时，可以使用显式 API：

```csharp
public async Task UpdateOrderAsync(Order order)
{
    _transactionManager.Begin();

    try
    {
        order.Status = 2;
        await _orders.UpdateAsync(order);

        _transactionManager.Commit();
    }
    catch
    {
        _transactionManager.Rollback();
        throw;
    }
}
```

当前 `ILocalTransactionManager` 没有 `CommitAsync`；异步业务中仍调用同步的 `Commit` 或 `Complete`。提交时框架会顺序保存上下文变更，数据库事务提交成功后再接受实体状态，因此不需要在业务代码中再调用一次 `SaveChanges`。

如果在上下文池为空时调用 `Begin()`，且没有传入 `ensureTransaction: true`，则不会创建数据库事务。此时直接 `Commit()` 会按历史兼容语义执行池保存，不能把这种调用当作事务使用。

## 嵌套工作单元

同一个事务管理器重复调用 `Begin` 或 `BeginScope` 只会增加逻辑深度，不会创建第二个数据库事务。内层完成只退出当前层级，真正的保存和提交延迟到最外层。

```csharp
await using var outer = _transactionManager.BeginScope();

await _orders.InsertAsync(order);

await using var inner = _transactionManager.BeginScope();
await _orders.UpdateAsync(order);
inner.Complete(); // 只完成内层深度

outer.Complete(); // 最外层才保存并提交
```

内层回滚会把整个事务标记为 `RollbackOnly`，外层不能继续提交。当前实现没有数据库级 `SAVEPOINT`，因此不能把嵌套回滚当作局部回滚后继续提交。

## EF Core SQL 与实体操作组合

EF Core 支持的原生 SQL 和批量更新可以与仓储操作放在同一个本地事务中：

```csharp
public async Task CloseOrdersAsync(Guid customerId)
{
    var db = Db.GetDbContext<OrdersDbContextLocator>();

    await using var transactionScope = _transactionManager.BeginScope();

    await db.Set<Order>()
        .Where(x => x.CustomerId == customerId)
        .ExecuteUpdateAsync(setters =>
            setters.SetProperty(x => x.Status, 2));

    await db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE order_audit SET updated_at = CURRENT_TIMESTAMP WHERE customer_id = {customerId}");

    await _orders.InsertAsync(new OrderAudit
    {
        CustomerId = customerId,
        Action = "close"
    });

    transactionScope.Complete();
}
```

`ExecuteUpdateAsync` 和 `ExecuteSqlInterpolatedAsync` 会立即向数据库发送命令，但在当前 `DbContext` 已绑定事务时，仍会随该事务提交或回滚。它们不会自动加入其他数据库连接的事务。

Core 不再提供独立的 ADO.NET 命令、`DataTable`、存储过程或 SQL 仓储封装。需要原生 SQL 时，业务代码直接使用 EF Core `DatabaseFacade` API 并传入参数；游标、流式读取、`OracleBulkCopy` 等 Provider 特有场景由对应子包负责。

## 多个 DbContext 与多个数据库

多个上下文只有在共享同一个物理 `DbConnection` 实例时，才能参加同一个本地事务。仅仅满足数据库类型相同、连接字符串相同或数据库名称相同都不够。

不同物理连接会形成多个独立的本地事务组。事务管理器会顺序保存并提交这些事务组，但不提供跨连接原子性：如果第一个连接已经提交而第二个连接提交失败，就会出现部分提交。Oracle 与 MySQL、主库与从库之间都不能由 Core 组成一个原子事务；跨库一致性应使用 Outbox、幂等和补偿流程。

## AutoSaveChanges 与事务管理器

`AutoSaveChanges` 当前只对实现 `IDynamicService` 的动态服务生效，普通 Controller、后台任务和消息消费不会自动保存。

| 场景 | 事务或保存的拥有者 |
| --- | --- |
| 动态服务，无 `[UnitOfWork]` | Action 成功后由 `AutoSaveChanges` 调用 `SavePoolNow` |
| 动态服务，有 `[UnitOfWork]` | `UnitOfWork` 过滤器开启、提交或回滚；自动保存过滤器跳过 |
| 动态服务，业务代码手动使用事务管理器 | 业务代码调用 `BeginScope/Complete` 或 `Begin/Commit` |
| 普通 Controller、后台任务、消息消费 | 业务代码显式使用事务管理器或显式调用保存 |

动态服务由业务代码手动提交时，建议使用 `[ManualCommit]`，明确禁止 Action 结束时的自动保存：

```csharp
[ManualCommit]
public async Task<IActionResult> ExecuteAsync()
{
    await using var transactionScope = _transactionManager.BeginScope();

    await _orders.InsertAsync(new Order());

    transactionScope.Complete();
    return Ok();
}
```

不要在同一个方法中让业务代码和 `AutoSaveChanges` 分别拥有提交责任。`[UnitOfWork]` 方法内部可以建立嵌套事务作用域，但最外层提交应由工作单元过滤器负责。

## UnitOfWork 与事务管理器的关系

`UnitOfWork` 不是另一套数据库事务实现，而是 MVC 过滤器层的自动编排入口：

```text
[UnitOfWork]
    -> UnitOfWorkFilter
        -> EFCoreUnitOfWork
            -> IDbContextPool
            -> DbContextPool 本地事务协调器
```

Web Action 需要由框架自动管理事务时，只需标记方法：

```csharp
[UnitOfWork]
public async Task<IActionResult> CreateOrderAsync(Order order)
{
    await _orders.InsertAsync(order);
    return Ok();
}
```

Action 正常返回时由工作单元过滤器提交，抛出异常时回滚。需要在没有预先解析默认上下文的场景中确保事务存在时，可以使用 `[UnitOfWork(true)]`。

`EFCoreUnitOfWork` 当前依赖兼容接口 `IDbContextPool` 调用 `BeginTransaction`、`CommitTransaction` 和 `RollbackTransaction`；默认 `DbContextPool` 同时实现公开的 `ILocalTransactionManager`。因此两者使用同一个作用域内的底层事务状态，但使用方式不同：

- Web Action 希望框架自动管理事务时，使用 `[UnitOfWork]`。
- 业务服务、后台任务或消息处理需要明确边界时，注入并使用 `ILocalTransactionManager`。
- 不要让两套代码同时承担最外层提交和回滚。

## `Now` 方法迁移

核心仓储中的单实体立即保存 API 已移除，包括 `InsertNow`、`SaveNow`、`UpdateNow`、`UpdateIncludeNow`、`UpdateExcludeNow`、`DeleteNow` 及其异步重载。

新代码只改变跟踪状态：

```csharp
await _orders.InsertAsync(order);
```

然后在明确的业务边界统一使用 `Complete`、`Commit`、`[UnitOfWork]` 或显式 `SaveChanges`。`SavePoolNow` 仍作为上下文池和自动保存基础设施保留，但不应被当作单实体“立即提交”业务 API 使用。
