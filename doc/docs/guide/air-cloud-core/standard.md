### 标准列表

命名空间: `Air.Cloud.Core`

概述：AppRealization 聚合框架中的各类“标准实现”（Standard），每个标准在运行时按“内部实现优先，其次默认实现”的顺序解析：`InternalRealization.X ?? DefaultRealization.X`。通过反射与依赖注入，可在不修改框架核心的前提下替换或扩展实现。

---

### 解析顺序与来源

- InternalRealization：偏向“应用内/用户自定义”的实现来源。
    - 多数成员通过 `AppCore.GetService<T>()` 从容器解析；少数为可被直接赋值的静态字段（如 `Output`、`Configuration`、`DomainExceptionHandler`、`AssemblyScanning`、`Jwt`、`Injection`）。
- DefaultRealization：框架内置的“默认实现/兜底实现”。
    - 有些提供现成实现（如 `DefaultAppOutputDependency`、`DefaultAppConfigurationDependency`、`DefaultJsonSerializerStandardDependency`、`DefaultCompressStandardDependency`、`DefaultTraceLogDependency`、`DynamicAppStoreDependency`）。
    - 有些为未实现，调用将抛出 `NotImplementedException`（如 `Container`、`Cache`、`RedisCache`、`Injection`、`KVCenter`、`ServerCenter`、`Queue`）。

---

### 公共静态属性

下表中的“解析链路”均遵循 `InternalRealization.X ?? DefaultRealization.X` 规则。

| 名称 | 类型 | 默认实现/行为 | 备注 |
|---|---|---|---|
| Output | `IAppOutputStandard`  | `DefaultAppOutputDependency` | 内容输出标准 |
| Container | `IContainerStandard` | 抛出 `NotImplementedException` | 容器标准实现 |
| Configuration | `IAppConfigurationStandard`  | `DefaultAppConfigurationDependency` | 系统配置标准 |
| DomainExceptionHandler | `IAppDomainExceptionHandlerStandard` | `DefaultAppDomainExcepetionHandlerDependency` | 全局异常处理 |
| AssemblyScanning | `IAssemblyScanningStandard` | `DefaultAssemblyScanningDependency` | 类库扫描标准 |
| Jwt | `IJwtHandlerStandard` | `DefaultJwtHandlerDependency` | JWT 处理 |
| Cache | `IAppCacheStandard` | 抛出 `NotImplementedException` | 缓存标准实现 |
| RedisCache | `IRedisCacheStandard` | 抛出 `NotImplementedException` | 需引入 Redis 模组 |
| Compress | `ICompressStandard`  | `DefaultCompressStandardDependency` | 压缩解压 |
| JSON | `IJsonSerializerStandard`  | `DefaultJsonSerializerStandardDependency` | JSON 序列化 |
| PID | `IPIDPlugin` | `AppRealization.AppPlugin.GetPlugin<IPIDPlugin>()` | 取决于插件工厂注册 |
| AppPlugin | `IAppPluginFactory` | `AppPluginFactory` | 插件工厂 |
| Injection | `IAppInjectStandard`  | 抛出 `NotImplementedException` | 系统注入标准实现 |
| KVCenter | `IKVCenterStandard`  | 抛出 `NotImplementedException` | 键值对中心未配置 |
| ServerCenter | `IServerCenterStandard`  | 抛出 `NotImplementedException` | 服务管理未配置 |
| TraceLog | `ITraceLogStandard` | `DefaultTraceLogDependency` | 日志追踪标准实现 |
| Queue | `IMessageQueueStandard`  | 抛出 `NotImplementedException` | 消息队列标准实现 |
| DynamicAppStore | `IDynamicAppStoreStandard` | `DynamicAppStoreDependency` | 动态应用加载 |

---

### 静态构造函数行为

`static AppRealization()` 会：

1. 获取 `InternalRealization` 的所有字段（`FieldInfo[]`）。
2. 对每个字段，基于 `AppCore.StandardTypes` 查找可赋值的实现类型（`item.FieldType.IsAssignableFrom(s) && s != item.FieldType`）。
3. 若发现多个实现：
     - 记录模块清单 `ModulesInformation`（用于输出调试信息）。
     - 通过 `Output.Error(...)` 报错：“{标准名} 标准具有多个实现模块”。
4. 若至少有 1 个实现：`Activator.CreateInstance(Types[0])` 创建第一个实现并赋值到对应字段。
5. 若无实现：跳过，保持默认的回退到 `DefaultRealization` 的逻辑。

提示：该流程使 InternalRealization 的可赋值字段（如 `Output/Configuration/Jwt/...`）可被“反射填充”。而基于容器解析的属性（`=> AppCore.GetService<T>()`）仍遵循 IoC 配置。

---

### 方法说明

- SetDependency'<'TStandard'>'(TStandard standard)
    - 作用：设置某个标准实现。
    - 机制：反射查找 `InternalRealization` 中字段类型与 `typeof(TStandard)` 完全一致的字段。
        - 找到则直接 `Field.SetValue(null, standard)`，覆盖内部实现。
        - 未找到则回退为 `AppCore.SetService(standard)`，向容器注册。
    - 注意：建议在 Startup 中调用；若在 Program.cs 过早调用，可能因资源尚未加载而导致空指针异常。
    - 切面：标注了 `[Aspect(typeof(ExecuteMethodPrinterAspectHandler))]`，可用于日志/审计等横切关注点。

- SetPlugin'<'TPlugin'>'(TPlugin plugin, string PluginName = null)
    - 作用：注册插件实例到插件工厂。
    - 机制：`return AppPlugin.SetPlugin(plugin, PluginName)`；`PluginName` 未提供时通常使用实例类型全名。
    - 切面：同上，带有执行日志切面。

---

### 自定义实现来源（InternalRealization）

- 多数成员通过 `AppCore.GetService<T>()` 从容器解析：
    - `Container`、`Cache`、`RedisCache`、`JSON`、`KVCenter`、`ServerCenter`、`TraceLog`、`Queue`、`DynamicAppStore`、`Compress`、`AppPlugin`、`PID` 等。
- 可被直接覆盖的静态字段（初始为 null，需要通过反射或 `SetDependency` 赋值）：
    - `Output`、`Configuration`、`DomainExceptionHandler`、`AssemblyScanning`、`Jwt`、`Injection`。

当对应 Internal 成员为 null 或容器解析失败时，将回退到 DefaultRealization。

---

### 框架默认实现 (DefaultRealization)

- 提供默认可用实现：
    - `Output` = `DefaultAppOutputDependency`
    - `Configuration` = `DefaultAppConfigurationDependency`
    - `JSON` = `DefaultJsonSerializerStandardDependency`
    - `Compress` = `DefaultCompressStandardDependency`
    - `TraceLog` = `DefaultTraceLogDependency`
    - `DynamicAppStore` = `DynamicAppStoreDependency`
    - `AppPlugin` = `AppPluginFactory`
- 未内置实现（调用将抛出未实现）：
    - `Container`、`Cache`、`RedisCache`、`Injection`、`KVCenter`、`ServerCenter`、`Queue`
- 特殊：
    - `PID` = `AppRealization.AppPlugin.GetPlugin<IPIDPlugin>()`，依赖插件工厂中的注册情况。

---

### 使用示例

注入或替换标准实现（建议在 Startup 中执行）：

```csharp
// 自定义输出实现
AppRealization.SetDependency<IAppOutputStandard>(new MyOutputDependency());

// 通过容器注册（当 InternalRealization 无匹配字段时由 SetDependency 回退到容器）
services.AddSingleton<IAssemblyScanningStandard, MyAssemblyScanningDependency>();

// 设置插件（名称可选，不填默认为类型全名）
AppRealization.SetPlugin<IPIDPlugin>(new MyPidPlugin(), "custom.pid");

// 运行时使用：统一从 AppRealization 获取
var logger = AppRealization.Output;
var json = AppRealization.JSON;
var config = AppRealization.Configuration;
```

---

### 注意事项与建议

- 调用时机：按注释建议，在 Startup 阶段设置依赖/插件，避免 Program.cs 过早执行导致空指针。
- 多实现冲突：若检测到同一标准存在多个实现，系统会通过 `Output.Error(...)` 报错并仅采用第一个实现；请确保同一标准仅保留唯一目标实现。
- 兜底策略：若 Internal 无法解析，将自动回退到 Default；若 Default 为未实现项，将抛出 `NotImplementedException`。
- 插件依赖：`PID` 的默认获取依赖 `AppPluginFactory` 的注册情况，未注册时的行为以插件工厂实现为准。

---

### 其他标准与扩展

以下标准并未通过 `AppRealization` 直接暴露属性，但作为框架标准的一部分，可通过 IoC 容器接入：

| 名称 | 接口/命名空间 | 获取方式 | 文档 |
|---|---|---|---|
| 分布式锁 | `Air.Cloud.Core.Standard.DistributedLock.IDistributedLockStandard` | 从 IoC 容器解析；如引入 `Air.Cloud.Modules.RedisCache` 模组，系统已提供实现并自动注入，可直接使用 | [分布式锁标准](docs/DistributedLock.md) |

