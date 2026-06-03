# 调度任务标准

调度任务标准用于抽象定时任务、周期任务和任务工厂能力。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ISchedulerStandardOptions` | 调度任务配置标准 |
| `ISchedulerStandardFactory<T>` | 调度任务工厂标准 |
| `ISchedulerExecutionCoordinatorStandard` | 分布式执行协调标准 |
| `ISchedulerExecutionRecordStandard` | 业务侧执行记录实体约束 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Quartz` | Quartz 调度任务实现 |

## 分布式唯一执行

当同一个服务部署多个实例时，每个实例都可能收到同一个 Quartz 触发。开启执行协调后，任务在真正执行业务逻辑前会先进入 `ISchedulerExecutionCoordinatorStandard` 做一次唯一裁决：

1. 根据服务名、任务分组、任务标识和计划触发时间生成 `ExecutionKey`。
2. 通过 `IDataRepositoryAccessor` 切换到业务实现的执行记录仓储。
3. 尝试插入一条执行记录；插入成功的实例获得执行权。
4. 如果唯一键冲突，则按租约和唯一策略判断是否允许接管。
5. 执行成功、失败或心跳时，协调器回写记录状态。

### 配置任务选项

```csharp
public sealed class DailyOrderScheduler : ISchedulerStandard<QuartzSchedulerStandardOptions>
{
    public QuartzSchedulerStandardOptions Options { get; } = new()
    {
        Id = "daily-order-sync",
        Name = "DailyOrderSync",
        Description = "每日订单同步",
        CronExpression = "0 0 1 * * ?",
        EnableExecutionCoordination = true,
        UniqueMode = SchedulerExecutionUniqueMode.PerFireTime,
        LeaseSeconds = 300,
        ExecutionRecordType = typeof(MySchedulerExecutionRecord)
    };
}
```

### 定义执行记录实体

业务侧自己定义表结构、迁移和索引，只需要实体实现 `ISchedulerExecutionRecordStandard`。生产环境必须为 `ExecutionKey` 建唯一索引，否则多个实例并发插入时无法获得数据库级唯一保证。

```csharp
public sealed class MySchedulerExecutionRecord : IPrivateEntity, ISchedulerExecutionRecordStandard
{
    public string ExecutionId { get; set; } = string.Empty;
    public string ExecutionKey { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public DateTimeOffset? ScheduledFireTimeUtc { get; set; }
    public string FireInstanceId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public SchedulerExecutionStatus Status { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset LeaseExpiresAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 唯一策略

| 策略 | 语义 | 适用场景 |
| --- | --- | --- |
| `Disabled` | 不做唯一执行裁决 | 本地开发或允许多实例同时执行 |
| `PerJob` | 同一个任务任意时刻只允许一个实例运行 | 任务不能重入，且执行时间可能超过触发间隔 |
| `PerFireTime` | 同一个任务的同一次计划触发只执行一次 | 常规周期任务，允许不同触发批次并行 |

### 运行前检查

- 应用需要注册具体的数据访问实现，例如 EFCore 模块会把现有 `IRepository<TEntity>` 适配为 `IDataRepository<TEntity>`。
- 多个执行记录实体同时存在时，需要显式设置 `ExecutionRecordType`，避免协调器扫描到多个候选类型。
- `LeaseSeconds` 应大于任务正常心跳或执行间隔；任务超时后，其他实例可以按策略接管。
- Quartz 自身的 AdoJobStore 集群模式仍建议开启；执行协调负责业务幂等，Quartz 集群负责触发器状态一致。

## 使用建议

- 周期任务应明确 Cron、并发策略和失败策略。
- 调度任务如果依赖业务服务，优先通过 DI 获取依赖，不要在 Job 内手动构建服务容器。
- 微服务多实例部署时，优先启用数据库唯一索引 + 执行协调，避免只依赖进程内状态。
