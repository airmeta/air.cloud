### KingbaseES V9 EF Core 适配

`Air.Cloud.EntityFrameWork.Kingbase` 为 Air.Cloud 数据库访问层提供 KingbaseES V9 的 EF Core Provider 封装。该类库从 `net-6.0.0.0` 分支迁移而来，并升级到 `net10.0`，当前接入 `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1`。

Short (en-us): `Air.Cloud.EntityFrameWork.Kingbase` provides the KingbaseES V9 EF Core provider adapter for the Air.Cloud database layer. It was migrated from the `net-6.0.0.0` branch and upgraded to `net10.0`, using `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1`.

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

### Provider 注册

在 EF Core 注册位置调用 `UseKingbase`：

```csharp
using Air.Cloud.EntityFrameWork.Kingbase.Extensions;

services.AddDbContext<DefaultDbContext>(options =>
{
    options.UseKingbase(
        Configuration["DataSourceSettings:ConnectionStrings:default"],
        "Your.Migrations.Assembly");
});
```

`UseKingbase` 内部调用 Kingbase Provider 的 `UseKdbndp(connectionString, options => ...)`。第二个参数 `migrationAssemblyName` 可为空；为空时使用 Kingbase Provider 默认迁移程序集行为。

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

### 注意事项

- `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `10.0.1` 是当前 `net10.0` 适配使用的 KingbaseES V9 Provider 包。
- 连接字符串会原样传递给 `UseKdbndp`，请按 KingbaseES V9 驱动要求配置服务器、端口、数据库、用户名和密码。
- `UseKingbase` 只负责 Provider 绑定和迁移程序集配置，不会自动创建数据库、打开连接或执行迁移。
- 如果需要接入 Air.Cloud 统一仓储/工作单元链路，应在具体业务项目中将 DbContext 注册到现有数据访问入口后再使用本 Provider 扩展。
