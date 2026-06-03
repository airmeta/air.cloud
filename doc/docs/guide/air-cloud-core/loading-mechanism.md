# 加载机制

加载机制是 `Air.Cloud.Core` 最核心的部分。理解它之后，模块为什么能自动注入、启动项为什么能排序、同一标准为什么不能有多个实现，都会比较清楚。

整体流程可以理解为：

```text
应用启动
  ↓
选择注入入口
  ↓
加载配置文件
  ↓
扫描程序集
  ↓
识别标准、模块、插件、启动项
  ↓
处理 AppStartup 启动顺序
  ↓
注册服务到 DI 容器
  ↓
填充 AppRealization 内部实现
  ↓
应用运行
```

## 1. 注入入口

不同类型应用有不同的注入入口。

Web 服务通常通过 `Air.Cloud.WebApp` 完成注入：

```csharp
builder.UseAirCloud();
```

Host 服务通常通过 `Air.Cloud.HostApp` 完成注入：

```csharp
hostBuilder.HostInjectInFile(assembly);
```

这两个入口背后都围绕 `IAppInjectStandard` 展开。

注意：同一应用中不要同时引入多个会实现 `IAppInjectStandard` 的直接入口，否则可能出现“标准具有多个实现模块”的冲突。

## 2. 配置加载

框架会先加载基础配置，再根据 `Environment` 决定加载哪个环境配置。

例如：

```json
{
  "Environment": "Development"
}
```

会加载：

```text
appsettings.json
appsettings.Development.json
```

如果有公共配置，还会加载：

```text
appsettings.Common.Development.json
```

配置加载机制详细见：`配置` 文档。

## 3. 程序集扫描

框架会扫描有效程序集，识别：

- 标准接口。
- 标准实现。
- 插件。
- `AppStartup` 启动类。
- 配置选项。
- 需要自动注册的服务。

这一步决定了“哪些模块能被框架发现”。

如果一个实现类没有被扫描到，通常要检查：

- 程序集是否被引用。
- 程序集是否被排除。
- 类型是否被 `[IgnoreScanning]` 忽略。
- 启动入口是否加载了正确的程序集。

## 4. AppStartup 启动顺序

模块或应用可以通过 `AppStartup` 声明启动逻辑。

启动项通常会包含：

- `ConfigureServices(IServiceCollection services)`
- `Configure(IApplicationBuilder app, IWebHostEnvironment env)`
- `Order`
- 依赖关系

框架会根据顺序和依赖关系执行启动项，保证基础能力先注册，依赖能力后注册。

## 5. AppRealization 标准解析

`AppRealization` 是标准实现的统一访问入口。

例如：

```csharp
var json = AppRealization.JSON;
var config = AppRealization.Configuration;
var queue = AppRealization.Queue;
```

解析规则大致是：

```text
InternalRealization.X ?? DefaultRealization.X
```

也就是说：

1. 优先使用内部实现或用户注入实现。
2. 如果没有，再使用默认实现。
3. 如果默认实现也没有，就抛出明确异常。

例如：

- `JSON` 有默认实现，可以直接用。
- `Configuration` 有默认实现，可以直接用。
- `Queue` 没有默认实现，必须引入 Kafka 等模块。
- `RedisCache` 没有默认实现，必须引入 Redis 模块。

## 6. 多实现冲突

如果同一个标准被扫描到多个实现，框架会认为这是冲突。

典型场景：

- 同时引入两个消息队列实现。
- 同时引入 WebApp 和 HostApp，导致多个 `IAppInjectStandard`。
- 自己写了一个实现，但没有移除默认模块实现。

处理方式：

- 保证一个标准只有一个目标实现。
- 如果只是替换实现，移除旧模块引用。
- 或者在启动阶段通过 `AppRealization.SetDependency<T>()` 明确指定。

---
