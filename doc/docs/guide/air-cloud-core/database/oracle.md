# Oracle Provider

`Air.Cloud.EntityFrameWork.Oracle` 提供 Oracle 的 EF Core Provider 适配，内部通过 `OracleDatabaseConfigure : IDatabaseConfigure` 调用 `UseOracle(...)`，并启用 Oracle 批量操作 SQL 生成器扩展。

## 安装

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.Oracle" Version="10.0.0" />
```

## 配置

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1 from dual",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "User Id=air_cloud;Password=your-password;Data Source=127.0.0.1:1521/ORCLPDB1;"
    }
  }
}
```

## 注册

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

services.AddDb<DefaultDbContext>(connectionMetadata: "default");
```

## 批量写入

Oracle 包包含 `BulkInsert` / `BulkInsertAsync` 扩展，面向当前 DbContext 映射表执行批量写入。

```csharp
await dbContext.BulkInsertAsync(entities);
```

## 注意事项

- Oracle 状态检查 SQL 通常使用 `select 1 from dual`。
- Oracle 批量操作扩展会替换部分 EF Core 查询 SQL 生成服务，升级 Oracle EF Core Provider 时要重点回归批量写入。
- 迁移程序集统一来自 `Db.MigrationAssemblyName`。
