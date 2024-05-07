## AppCore.cs 
应用程序核心类,具有一系列加载应用程序内容项、配置项的方法;

此外还存储框架核心信息,例如: 服务集合信息,程序集信息,关键类型信息等,并提供这些信息给其他类库使用
``` csharp
  /// <summary>
  /// <para>zh-cn:应用程序核心</para>
  /// <para>en-us:Application core</para>
  /// </summary>
  public static partial class AppCore
  {

  }
``` 

## 常量
| Name      | Remark | 
| ----------- | ----------- |
| ASSEMBLY_MODULES_KEY      | 模块关键词       |
| ASSEMBLY_PLUGINS_KEY      | 插件关键词       | 
| ASSEMBLY_ENHANCE_KEY      | 增强关键词       |
---
## 属性
|Scope|Static |  Name      | Type | Description|
|-----------|-----------| ----------- | ----------- |-----------|
|public|√|     CrucialAssemblies     |      IEnumerable\<AssemblyName>     | 关键类库(包含核心库,模组,插件,增强实现) |
|public|√|     Assemblies            |       IEnumerable\<AssemblyName>     | 所有应用程序集信息 |
|public|√|     CoreAssemblyName      |    AssemblyName       |  核心程序集名称|   
|public|√|     UnmanagedObjects      |       ConcurrentBag<IDisposable>    | 未托管的对象集合 |
|public|√|     CrucialTypes          |     IEnumerable\<Type>      |  关键类型 |
|public|√|     EffectiveTypes        |      IEnumerable\<Type>     | 有效类型 |
|public|√|     ModuleTypes           |        IEnumerable\<Type>    | 所有的模组 |
|public|√|     PluginTypes           |      IEnumerable\<Type>     | 所有的插件 |
|public|√|     EnhanceTypes          |      IEnumerable\<Type>      | 所有的增强 |
|public|√|     StandardTypes         |       IEnumerable\<Type>     | 所有的标准 |
|public|√|     Modules               |      IEnumerable\<AssemblyName>     | 所有的模组 |
|public|√|     Plugins               |     IEnumerable\<AssemblyName>      | 所有的插件 |
|public|√|     Enhances              |     IEnumerable\<AssemblyName>      | 所有的增强实现 |
|public|√|     EntityTypes           |        IEnumerable\<Type>    | 数据库实体类引用(在没有使用数据库模块的情况下 该属性为空集合) |
|public|√|     Configuration         |       IConfiguration    | 应用程序配置信息 |
|public|√|     Settings              |       AppSettingsOptions    |  应用程序全局配置|
|public|√|     RootServices          |       IServiceProvider    | 存储根服务,可能为空 |
|public|√|     InternalServices      |       IServiceCollection    | 内部服务集合 |
|public|√|     HttpContext      |       HttpContext    | 请求上下文(只在启用了WebApp模块中才可以使用,否则为null) |
|public|√|     AppStartType      |       AppStartupTypeEnum    | 应用程序启动类型:Host/Web |
|public|√|     StartTypes            |      IEnumerable\<Type>      | 所有的项目启动项 |
|public|√|     AppStartups      |       ConcurrentBag<AppStartup>    |  应用所有启动类|
|public|√|     ExternalAssemblies      |       IEnumerable<Assembly>    | 外部程序集(后续将会加入此功能,允许动态装配系统) |

---

## 方法
|Scope|Static | MethodName      | Description | 
|-----------|-----------| ----------- | ----------- |
|public|√|  AppCore      | 初始化Air.Cloud框架的核心信息       | 
|public|√|  GetServiceProvider      | 解析服务提供器       | 
|public|√|  GetService      |获取请求生存周期的服务       |
|public|√|  GetRequiredService      | 获取请求生存周期的服务       | 
|public|√|  GetOptions      | 获取选项       ||
|public|√|  GetOptionsMonitor      | 获取选项       |  
|public|√|  GetOptionsSnapshot      | 获取选项       | 
|public|√|  DisposeUnmanagedObjects      | 释放UnmanagedObjects对象       | 

### 方法详解
---
#### AppCore
* Code: 
``` csharp
static AppCore()
```
* Description:初始化Air.Cloud框架的核心信息
* MethodType:构造方法

####  GetServiceProvider
* Code: 
``` csharp
    public static IServiceProvider GetServiceProvider(Type serviceType)
```
* Description:解析服务提供器
* MethodType:静态方法
* Return:[IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
* Params:
  * Param1:
    * Name: serviceType
    * Type: Type
    * Description: 服务类型信息
#### GetService\<TService>

* Code: 
``` csharp
public static TService GetService<TService>(IServiceProvider serviceProvider = default)
       where TService : class
```
* Description: 获取请求生存周期的服务
* GenericType: 服务类型
* MethodType: 静态方法
* Return:服务
* Params:
  * Param1
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetService
* Code: 
``` csharp
public static object GetService(Type type, IServiceProvider serviceProvider = default)
```
* Description: 获取请求生存周期的服务
* MethodType: 静态方法
* Return: 服务(object类型)
* Params:
  * Param1:
    * Name: type
    * Type: Type
    * Description: 服务类型信息
  * Param2:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetRequiredService\<TService>
* Code: 
``` csharp
public static TService GetRequiredService<TService>(IServiceProvider serviceProvider = default)
       where TService : class
```
* Description: 获取请求生存周期的服务
* GenericType: 服务类型
* MethodType: 静态方法
* Return: 服务
* Params:
    * Param1:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetRequiredService
* Code: 
``` csharp
public static object GetRequiredService(Type type, IServiceProvider serviceProvider = default)
```
* Description: 获取请求生存周期的服务
* MethodType: 静态方法
* Return: 服务(object类型)
* Params:
  * Param1:
    * Name: type
    * Type: Type
    * Description: 服务类型信息
  * Param2:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetOptions\<TOptions>
* Code: 
``` csharp
 public static TOptions GetOptions<TOptions>(IServiceProvider serviceProvider = default) 
        where TOptions : class, new()
```
* Description: 获取选项
* GenericType: 强类型选项类型
* MethodType: 静态方法
* Return: 选项
* Params:
    * Param1:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetOptionsMonitor\<TOptions>
* Code: 
``` csharp
 public static TOptions GetOptionsMonitor<TOptions>(IServiceProvider serviceProvider = default) 
        where TOptions : class, new()
```
* Description: 获取选项
* GenericType: 强类型选项类型
* MethodType: 静态方法
* Return: 选项
* Params:
    * Param1:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器

#### GetOptionsSnapshot\<TOptions>
* Code: 
``` csharp
 public static TOptions GetOptionsSnapshot<TOptions>(IServiceProvider serviceProvider = default) 
        where TOptions : class, new()
```
* Description: 获取选项
* GenericType: 强类型选项类型
* MethodType: 静态方法
* Return: 选项
* Params:
    * Param1:
    * Name: serviceProvider
    * Type: [IServiceProvider](https://learn.microsoft.com/zh-cn/dotnet/api/system.iserviceprovider?view=net-6.0)
    * Description: 服务提供器
