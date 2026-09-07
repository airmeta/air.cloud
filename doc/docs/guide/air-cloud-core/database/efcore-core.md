# EF Core 核心包

`Air.Cloud.EntityFrameWork.Core` 是 Air.Cloud 的 EF Core 数据访问基础包，提供 DbContext 注册、仓储、连接字符串解析、数据库状态检查和 EF Core 模型元数据扩展。

## 安装

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.Core" Version="10.0.2" />
```

Provider 包会引用核心包；业务项目通常还需要引用一个具体数据库 Provider 包。

## 连接配置

数据库连接配置统一位于 `DataSourceSettings:ConnectionStrings`，最终仍以原始字符串传递给 EF Core Provider：

```json
{
  "DataSourceSettings": {
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "Server=127.0.0.1;Database=air_cloud;User Id=root;Password=your-password;"
    }
  }
}
```

| 配置项 | 说明 |
| --- | --- |
| `ConnectionValidationSQL` | 兼容旧配置，仅用于决定是否启用状态检查；检查本身使用 EF Core `CanConnectAsync` |
| `ConnectionValidationIntervalMillis` | 状态检查间隔，单位毫秒 |
| `ConnectionStrings` | Air.Cloud 正式命名连接字符串集合，键为连接名称，值为原始连接字符串 |

## 注册 DbContext

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Microsoft.EntityFrameworkCore;

public sealed class DefaultDbContext : DbContext
{
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options)
        : base(options)
    {
    }
}

services.AddDb<DefaultDbContext>(connectionMetadata: "default");
```

连接池版本：

```csharp
services.AddDbPool<DefaultDbContext>(
    connectionMetadata: "default",
    poolSize: 128);
```

多 DbContext 定位器：

```csharp
public sealed class ReportingDbContextLocator : IDbContextLocator
{
}

services.AddDb<ReportingDbContext, ReportingDbContextLocator>(
    connectionMetadata: "reporting");
```

## 仓储入口

```csharp
var repository = Db.GetRepository<OrderEntity>();
var dbRepository = Db.GetDbRepository<MasterDbContextLocator>();
```

Core 不再注册或暴露独立 SQL/ADO.NET 仓储。需要执行 EF Core 支持的原生 SQL 时，直接使用 `repository.Database` 上的 EF Core API，并由业务代码负责参数和事务边界；DataTable、存储过程游标、批量复制等 ADO.NET 或 Provider 特有场景应由对应子包实现。

## 模型元数据扩展

核心包提供 `IColumnMetadataProvider`，用于在 EF Core `OnModelCreating` 阶段统一改列类型、列名等关系型元数据。详见 [列元数据提供器](./column-metadata-provider.md)。

## 注意事项

- 核心包不绑定具体数据库；必须引入一个 Provider 包，或手动注册 `IDatabaseConfigure`。
- `AddDb` / `AddDbPool` 会先由数据源配置解析器绑定 `DataSourceOptions` 并读取连接名称，再通过当前 `IDatabaseConfigure` 把原始连接字符串应用到 EF Core Provider；`DbProvider` 只负责选择上下文特性中的连接元数据。
- 如果启用了动态模型，模型缓存键会切换到动态缓存实现。

### 连接配置迁移

新项目只应使用 `DataSourceSettings:ConnectionStrings:<name>`。旧版本支持官方根节点 `ConnectionStrings:<name>`、完整配置路径以及直接传入连接字符串，但这些形式仅用于迁移兼容；未解析的名称目前仍按历史行为原样透传，后续主版本将改为启动期直接报错。
