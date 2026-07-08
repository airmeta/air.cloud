# MySQL Provider

`Air.Cloud.EntityFrameWork.MySQL` 提供 MySQL 的 EF Core Provider 适配，内部通过 `MySQLDatabaseConfigure : IDatabaseConfigure` 调用 `UseMySql(...)`。

## 安装

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.MySQL" Version="10.0.0" />
```

## 配置

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "Server=127.0.0.1;Port=3306;Database=air_cloud;Uid=root;Pwd=your-password;"
    }
  }
}
```

## 注册

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

services.AddDb<DefaultDbContext>(connectionMetadata: "default");
```

`MySQLDatabaseConfigure` 使用 `ServerVersion.AutoDetect(connectionMetadata)` 自动识别 MySQL 版本，并使用 `Db.MigrationAssemblyName` 作为迁移程序集。

## 注意事项

- `ConnectionValidationSQL` 建议使用 `select 1`。
- `ServerVersion.AutoDetect` 会依赖连接字符串访问数据库识别版本；如果环境不允许启动时探测，应考虑自定义 `IDatabaseConfigure`。
- 同一个应用不要同时无控制地引入多个关系型 Provider 包。
