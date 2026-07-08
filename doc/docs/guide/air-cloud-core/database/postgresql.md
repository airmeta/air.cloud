# PostgreSQL Provider

`Air.Cloud.EntityFrameWork.PostgreSQL` 提供 PostgreSQL 的 EF Core Provider 适配，内部通过 `PostgreSQLDatabaseConfigure : IDatabaseConfigure` 调用 `UseNpgsql(...)`。

## 安装

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.PostgreSQL" Version="10.0.0" />
```

## 配置

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "Host=127.0.0.1;Port=5432;Database=air_cloud;Username=postgres;Password=your-password;"
    }
  }
}
```

## 注册

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

services.AddDb<DefaultDbContext>(connectionMetadata: "default");
```

`PostgreSQLDatabaseConfigure` 会把 `Db.MigrationAssemblyName` 传给 Npgsql 的迁移程序集配置。

## 注意事项

- PostgreSQL 和 Kingbase 的连接字符串形态相近，但 Provider 包不同，不能混用。
- 如果项目需要 schema、类型映射或时间类型策略，应优先通过 DbContext 模型配置或 `IColumnMetadataProvider` 统一处理。
