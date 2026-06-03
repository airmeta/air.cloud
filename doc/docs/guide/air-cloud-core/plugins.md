# 插件

插件（Plugin）用于增强应用能力，通常作用于 Web/API 层，例如鉴权、接口文档、API 目录、代码生成等。

插件和模块的区别是：模块更偏运行时基础能力，插件更偏应用增强能力。没有某个插件，服务通常仍然可以运行；没有必要模块，相关标准能力可能会不可用。

---

## 当前插件列表

| 类库 | 主要用途 | 典型场景 |
| --- | --- | --- |
| `Air.Cloud.Plugins.Jwt` | JWT 鉴权 | 登录态校验、Token 解析、权限入口 |
| `Air.Cloud.Plugins.SpecificationDocument` | 接口文档 | Swagger / OpenAPI 文档生成 |
| `Air.Cloud.Plugins.APICatalog` | API 目录 | API 探针、接口目录、服务接口收集 |
| `Air.Cloud.Plugins.CodeGenerator` | 代码生成 | 根据元数据生成代码或模板 |
| `Air.Cloud.Plugins.Security` | 安全能力 | MD5 等安全辅助能力 |

---

## 插件二级文档

常用 Web/API 插件已拆到独立页面维护：

- [APICatalog](./plugins/api-catalog.md)：APIProbe、接口目录、标准元数据、Provider 扩展。
- [JWT](./plugins/air_jwt.md)：JwtBearer 注册、Token 生成与刷新、鉴权处理器。
- [Swagger](./plugins/air_swagger.md)：Swagger JSON、Swagger UI、OpenAPI 分组、Swagger Provider。

API 目录和接口文档的边界：

- `Air.Cloud.Plugins.APICatalog` 面向框架标准 API 目录，核心抽象是 `IAPIProbeProvider`。
- `Air.Cloud.Plugins.SpecificationDocument` 面向 Swagger / OpenAPI 文档生成，也可提供 `SwaggerAPIProbeProvider`。
- `Air.Cloud.WebApp` 只生成 MVC 应用模型和标准元数据，不直接绑定 Swagger、Swashbuckle 或 OpenAPI。

---

## 插件工厂

插件由 `IAppPluginFactory` 管理。默认工厂可以通过 `AppRealization.AppPlugin` 获取。

注册插件：

```csharp
AppRealization.SetPlugin<IMyPlugin>(new MyPlugin());
```

获取插件：

```csharp
var plugin = AppRealization.AppPlugin.GetPlugin<IMyPlugin>();
```

如果注册时传入插件名，获取时也要使用相同名称。没有传插件名时，通常使用类型全名作为默认名称。

---

## 插件加载方式

插件有两种常见加载方式。

### 1. NuGet 或项目引用

这是最常用方式。项目引用插件包后，在启动阶段调用插件提供的扩展方法：

```csharp
builder.Services.AddSpecificationDocuments();
builder.Services.AddJwtAuthentication();
```

实际方法名以插件源码为准。

### 2. 动态插件目录

动态插件目录是应用运行目录下的：

```text
Plugins/{插件名}/{插件名}.dll
```

默认加载流程：

1. `DynamicAppStoreDependency` 扫描 `Plugins` 目录。
2. 按子目录名称查找同名 DLL。
3. 使用 `AssemblyLoadContext` 加载插件程序集。
4. 扫描插件中的过滤器、中间件、`PluginInjectAttribute` 标记类型。
5. 将扫描结果写入动态应用扫描结果集合。
6. 应用启动时按插件用途装配到 Web 管道或 MVC 能力中。

动态插件适合独立发布、按需放置的扩展能力。核心业务插件更建议用普通引用方式接入，便于版本管理。

---

## PluginInjectAttribute

插件可以使用 `PluginInjectAttribute` 描述加载顺序和用途。框架会读取：

- `Usage`：插件用途，例如过滤器、中间件等。
- `OrderType`：排序类型。
- `Order`：排序数值。

如果插件类型没有该特性，但实现了 `IAsyncActionFilter` 或类型名以 `MiddleWare` 结尾，也可能被动态插件扫描逻辑识别。

---

## 插件适合做什么

适合放在插件里的能力：

- 请求过滤器。
- 中间件。
- API 文档生成。
- 鉴权入口。
- 启动 Banner 或应用标识。
- API 探针、目录、元数据收集。

不建议放在插件里的能力：

- 复杂业务流程。
- 强依赖数据库的领域服务。
- 必须保证事务一致性的操作。
- 大量后台消费或调度任务。

这些更适合放在业务项目或模块中。

---

## 自定义插件示例

```csharp
public interface IMyPlugin : IPlugin
{
    string GetName();
}

public class MyPlugin : IMyPlugin
{
    public string GetName()
    {
        return "MyPlugin";
    }
}

AppRealization.SetPlugin<IMyPlugin>(new MyPlugin());
```

使用：

```csharp
var plugin = AppRealization.AppPlugin.GetPlugin<IMyPlugin>();
var name = plugin.GetName();
```

---

## 覆盖默认插件

如果业务侧需要替换默认插件，推荐在应用启动阶段注册自己的实现：

```csharp
[AppStartup]
public class PluginOverrideStartup : AppStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        AppRealization.SetPlugin<IAppBannerPlugin>(new MyBannerPlugin());
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
```

不要在业务调用点临时替换插件。插件属于全局能力，应该在启动阶段确定。

---

## 排查插件未生效

如果插件没有生效，按下面顺序检查：

1. 插件包是否被引用，或动态插件 DLL 是否放在正确目录。
2. 动态插件目录结构是否满足 `Plugins/{插件名}/{插件名}.dll`。
3. 插件类型是否实现了正确接口或标记了 `PluginInjectAttribute`。
4. 是否调用了插件注册扩展方法。
5. 插件配置节是否开启。
6. 是否被业务自定义插件覆盖。
