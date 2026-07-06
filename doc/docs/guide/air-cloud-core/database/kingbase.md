### KingbaseES V9 EF Core 适配

`Air.Cloud.EntityFrameWork.Kingbase` 为 Air.Cloud 数据库访问层提供 KingbaseES V9 的 EF Core Provider 适配。当前主线面向 `net10.0`，接入 `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1`，并通过 `IDatabaseConfigure` 接入 Air.Cloud 统一数据库配置入口。

Short (en-us): `Air.Cloud.EntityFrameWork.Kingbase` provides the KingbaseES V9 EF Core provider adapter for the Air.Cloud database layer. The current main line targets `net10.0`, uses `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1`, and integrates through the unified Air.Cloud `IDatabaseConfigure` database entry point.

---

### 安装与引用

在需要访问 KingbaseES V9 的项目中引用：

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.Kingbase" Version="10.0.0" />
```

源码开发时也可以引用项目：

```xml
<ProjectReference Include="..\..\src\db\Air.Cloud.EntityFrameWork.Kingbase\Air.Cloud.EntityFrameWork.Kingbase.csproj" />
```

---

### 统一入口注册

Kingbase 包提供 `KingbaseDatabaseConfigure : IDatabaseConfigure`，模块启动时注册：

```csharp
services.AddSingleton<IDatabaseConfigure, KingbaseDatabaseConfigure>();
```

业务项目使用 Air.Cloud 数据库核心的 `AddDb` 或 `AddDbPool` 注册 DbContext。连接元数据会先通过 `DbProvider.GetConnectionString<TDbContext>(...)` 解析，然后交给 `KingbaseDatabaseConfigure.Configure<TDbContext>(...)`。

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

services.AddDb<DefaultDbContext>(
    connectionMetadata: "default");
```

如需设置迁移程序集，在接入数据库访问器时设置 Air.Cloud 数据库核心的迁移程序集名称；Kingbase 配置器会使用 `Db.MigrationAssemblyName`：

```csharp
services.AddDatabaseAccessor(
    migrationAssemblyName: "Your.Migrations.Assembly");
```

---

### 配置示例

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=your-password;"
    }
  }
}
```

```csharp
using Microsoft.EntityFrameworkCore;

public class DefaultDbContext : DbContext
{
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options)
        : base(options)
    {
    }
}
```

---

### 底层扩展

`UseKingbase` 仍然保留为底层 EF Core 扩展，供高级场景直接配置 Provider：

```csharp
using Air.Cloud.EntityFrameWork.Kingbase.Extensions;

services.AddDbContext<DefaultDbContext>(options =>
{
    options.UseKingbase(
        "Server=127.0.0.1;Port=54321;Database=air_cloud;User Id=system;Password=your-password;",
        "Your.Migrations.Assembly");
});
```

常规 Air.Cloud 项目应优先使用 `IDatabaseConfigure` 统一入口，避免绕过框架的连接字符串解析、DbContext 定位器、动态模型缓存键和状态检查链路。

---

### 注意事项

- `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1` 是当前 `net10.0` 适配使用的 KingbaseES V9 Provider 包。
- `KingbaseDatabaseConfigure` 会把连接元数据作为 Kingbase 连接字符串传递给 `UseKdbndp`。
- 迁移程序集统一来自 `Db.MigrationAssemblyName`，和 MySQL、Oracle、PostgreSQL 适配器保持一致。
- `UseKingbase` 和 `KingbaseDatabaseConfigure` 只负责 Provider 绑定和迁移程序集配置，不会自动创建数据库、打开连接或执行迁移。
- 如果启用了 `DataSourceSettings.ConnectionValidationSQL`，模块启动注册会接入数据库状态检查后台服务。
