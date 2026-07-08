# EF Core 核心包

`Air.Cloud.EntityFrameWork.Core` 是 Air.Cloud 的 EF Core 数据访问基础包，提供 DbContext 注册、仓储、SQL 仓储、连接字符串解析、数据库状态检查和 EF Core 模型元数据扩展。

## 安装

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.Core" Version="10.0.2" />
```

Provider 包会引用核心包；业务项目通常还需要引用一个具体数据库 Provider 包。

## 连接配置

配置节点为 `DataSourceSettings`：

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "Server=127.0.0.1;Database=air_cloud;User Id=root;Password=your-password;"
    }
  }
}
```

| 配置项 | 说明 |
| --- | --- |
| `ConnectionValidationSQL` | 数据库状态检查 SQL。为空时不启用状态检查后台服务 |
| `ConnectionValidationIntervalMillis` | 状态检查间隔，单位毫秒 |
| `ConnectionStrings` | 命名连接字符串集合 |

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
var sqlRepository = Db.GetSqlRepository<MasterDbContextLocator>();
```

## 模型元数据扩展

核心包提供 `IColumnMetadataProvider`，用于在 EF Core `OnModelCreating` 阶段统一改列类型、列名等关系型元数据。详见 [列元数据提供器](./column-metadata-provider.md)。

## 注意事项

- 核心包不绑定具体数据库；必须引入一个 Provider 包，或手动注册 `IDatabaseConfigure`。
- `AddDb` / `AddDbPool` 会通过当前 `IDatabaseConfigure` 把连接元数据应用到 EF Core Provider。
- 如果启用了动态模型，模型缓存键会切换到动态缓存实现。
