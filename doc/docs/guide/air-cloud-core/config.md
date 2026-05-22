### 环境与配置

框架内包含一套自有的逻辑来处理配置文件与运行环境之间的关系，使用 Environment 配置项来指定你想要运行的环境。

#### 配置文件加载机制

我们知道，默认创建的 WebAPI 项目包含一些默认的 appsettings.json 配置文件：

例如：

```
appsettings.json
appsettings.Development.json
```

但是在 Air.Cloud 中，我们打破了这种常规的限制，Environment 配置决定了你使用哪个配置文件。

例如：在 appsettings.json 中配置如下

``` json
{
  "Environment":"Development"
}
```

那么这个时候就会加载 `appsettings.json` 和 `appsettings.Development.json` 作为项目的配置文件。

根据此特性，你可以配置 N 个不同环境的配置文件。

例如：

``` json
{
  "Environment":"Test/Production"
}
```

这将会分别加载 `appsettings.Test.json` 或者 `appsettings.Production.json`。

#### 内置的环境

框架内置三个环境，分别是 Development、Test、Production，优先级数字越小越先执行。

| 优先级    | 环境标识      | 描述 |特殊逻辑 |
| ----------- | ----------- | ----------- |----------- |
| 1 |    Development   | 开发环境      |  如果本地调试时将会默认使用该配置，通过强制指定 Environment 配置时，AppEnvironment.IsDevelopment 总是为 true  |
| 2 |    Test   | 测试环境      |  如果运行路径中包含关键词 test，将会自动指向到此环境，这可以避免由于操作不当导致的生产与测试环境混乱  |
| 3 |    Production   | 生产环境      |   如果 Debugger.IsAttach 和运行路径中不存在关键词 test，将会使用该环境 |

::: warning 注意
手动指定的环境优先级总是为 0，总是最先执行
:::

#### 特殊的环境

::: danger 提示
你无法指定 Common 关键词作为环境标识
:::

在 Air.Cloud 框架中 Common 作为环境加载引导标识存在，系统在启动时不仅仅加载服务本身的配置，另外还加载公共配置。

公共配置文件的命名为：

```
appsettings.Common.{Environment}.json
```

#### 配置示例

**开发环境配置** (`appsettings.Development.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

**测试环境配置** (`appsettings.Test.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Redis": {
    "ConnectionString": "test-redis:6379"
  }
}
```

**生产环境配置** (`appsettings.Production.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "Redis": {
    "ConnectionString": "prod-redis:6379,password=xxx"
  }
}
```

**公共配置** (`appsettings.Common.Development.json`):

```json
{
  "Common": {
    "ServiceName": "Air.Cloud.Service",
    "Version": "1.0.0"
  }
}
```

#### 核心代码片段

```csharp
/// <summary>
/// 当前程序虚拟运行环境
/// </summary>
/// <returns></returns>
internal static EnvironmentEnums VirtualEnvironment()
{
    if (AppConst.EnvironmentStatus.HasValue) return AppConst.EnvironmentStatus.Value;
    AppConst.IsDebugger = Debugger.IsAttached;
    string ConfigEnovriment = AppConfigurationLoader.InnerConfiguration[AppConst.ENVIRONMENT];
    if (!string.IsNullOrEmpty(ConfigEnovriment) && ConfigEnovriment.ToUpper() == "COMMON")
        throw new Exception("无法指定环境标识为Common，请更换环境标识");
    if (!string.IsNullOrEmpty(ConfigEnovriment))
    {
        var Result = Enum.TryParse(ConfigEnovriment, out EnvironmentEnums env);
        if (Result) AppConst.EnvironmentStatus = env;
        if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Other;
        return AppConst.EnvironmentStatus.Value;
    }
    return RealEnvironment();
}

/// <summary>
/// 当前程序真实运行环境
/// </summary>
/// <returns></returns>
internal static EnvironmentEnums RealEnvironment()
{
    //调试模式
    if (AppConst.IsDebugger)
    {
        if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Development;
        return AppConst.EnvironmentStatus.Value;
    }
    //测试模式
    var IsTest = AppConst.ApplicationPath?.ToLower().Contains(AppConst.ENVIRONMENT_TEST_KEY);
    if (IsTest.HasValue && IsTest.Value)
    {
        if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Test;
        return AppConst.EnvironmentStatus.Value;
    }
    //生产模式
    if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Production;
    return AppConst.EnvironmentStatus.Value;
}
```

#### 配置最佳实践

1. **环境隔离**: 使用不同的配置文件确保开发、测试、生产环境完全隔离
2. **敏感信息**: 生产环境配置中的敏感信息使用环境变量或密钥管理服务
3. **公共配置**: 使用 Common 配置文件存放所有环境共用的配置项
4. **版本控制**: 开发环境配置可以提交到版本控制，生产环境配置不应提交
5. **配置验证**: 在应用启动时验证配置项的完整性

#### 相关文档

- [标准列表](/guide/air-cloud-core/standard) - 了解框架的标准接口
- [模组列表](/guide/air-cloud-core/libs) - 查看可用的模组
