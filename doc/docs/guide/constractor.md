### 设计理念

在普遍的框架设计中，框架中支持的功能都会具有默认的实现，并且在一定程度上会非常的臃肿不适用于扩展。

在 Air.Cloud 中，Air.Cloud.Core 仅包含功能接口定义，我们把这部分定义称作为 **Standard（标准）**，除了一些特殊的标准需要默认定义之外，其他标准仅包含接口。

还有一部分是**插件**，框架内提供了一些默认的插件，你可以把它当作为以前我们使用的 Util 类。

![架构图](/assets/1.png)

基本上 Air.Cloud 所有模块都是可以从 NuGet 中找寻替换版本或者自行实现相关功能，并且我们也欢迎你贡献独属于你自己的实现。

#### 标准与实现的关系

```
┌─────────────────────────────────────────┐
│            Air.Cloud.Core                │
│         (定义标准接口 Standard)          │
│  ┌───────────────────────────────────┐  │
│  │  IAppCacheStandard                │  │
│   └───────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │ 实现
               ▼
┌─────────────────────────────────────────┐
│         Modules (实现标准)               │
│  ┌───────────────────────────────────┐  │
│  │  RedisCacheDependency            │  │
│  │  MemoryCacheDependency            │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### 设计优点

1. **多态** - 屏蔽了实现对现有业务代码的影响，在多个标准实现之间可以随意切换而不需要调整代码

2. **简洁** - 简化框架核心的臃肿代码，减少了框架对于其他 NuGet 包的引用

3. **快速** - 由于框架核心不实现任何功能，框架的加载速度会非常快

4. **轻量** - 发布包的大小随着你对于实现的引用而变化，默认只有十几兆左右即可运行

5. **自由** - 更好的支持，不必纠结任何实现的问题，实现有问题你就重新编写，自行注入，不需要等作者修复而带来的额外时间消耗

### 设计缺点

1. **历史遗留** - 框架是基于 Furion v3.8.6(MIT) 的部分代码余烬中新生，包含了一部分旧代码，但是该部分已经抽取成单独的类库，后续 v2.0 阶段将会将其优化

2. **发展局限** - 框架目前仅支持对于 WebAPI 方向，后续 v2.0 将会支持更多的功能

### 使用场景

#### 场景一：切换缓存实现

假设项目当前使用 Redis 缓存，现在需要切换到内存缓存（用于测试环境）：

```csharp
// 原来的 Redis 缓存实现
services.AddSingleton<IAppCacheStandard, RedisCacheDependency>();

// 切换到内存缓存（不需要修改业务代码）
services.AddSingleton<IAppCacheStandard, MemoryCacheDependency>();

// 业务代码保持不变
var cache = AppRealization.Cache;
cache.Set("key", "value");
var value = cache.Get<string>("key");
```

#### 场景二：自定义日志实现

```csharp
// 定义自定义日志实现
public class CustomLogDependency : ITraceLogStandard
{
    public void Log(string message, LogLevel level)
    {
        // 自定义日志逻辑
        Console.WriteLine($"[{level}] {message}");
    }
}

// 注入自定义实现
AppRealization.SetDependency<ITraceLogStandard>(new CustomLogDependency());
```

#### 场景三：替换消息队列

```csharp
// 使用 Kafka 消息队列
services.AddSingleton<IMessageQueueStandard, KafkaMessageQueueDependency>();

// 或者使用 RabbitMQ 消息队列
services.AddSingleton<IMessageQueueStandard, RabbitMQMessageQueueDependency>();

// 业务代码保持不变
var messageQueue = AppRealization.Queue;
messageQueue.Publish(config, message);
```

### 实现最佳实践

1. **命名规范**
   - 标准接口命名：`I{功能名}Standard`
   - 实现类命名：`{功能名}Dependency`

2. **依赖注入**
   - 优先使用 `services.AddSingleton` 注册标准实现
   - 避免使用 `services.AddTransient`，除非有特殊需求

3. **扩展性**
   - 保留标准接口的扩展点
   - 提供合理的默认值
   - 支持配置化

4. **文档**
   - 为每个标准编写清晰的文档
   - 提供使用示例
   - 说明注意事项

### 相关文档

- [标准列表](/guide/air-cloud-core/standard) - 查看所有可用的标准接口
- [模组列表](/guide/air-cloud-core/libs) - 查看所有可用的模组实现
- [插件列表](/guide/air-cloud-core/plugins) - 查看所有可用的插件
