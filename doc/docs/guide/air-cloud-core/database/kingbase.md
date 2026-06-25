### KingbaseES V9 EF Core 适配

`Air.Cloud.EntityFrameWork.Kingbase` 为 Air.Cloud 数据库访问层提供 KingbaseES V9 的 EF Core Provider 封装。该类库接入 `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `6.0.1.808`，并提供 `UseAirCloudKingbase` 扩展把业务 `DbContext` 绑定到 KingbaseES。

Short (en-us): `Air.Cloud.EntityFrameWork.Kingbase` provides the KingbaseES V9 EF Core provider adapter for the Air.Cloud database layer. It integrates `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `6.0.1.808` and wires business `DbContext` instances through the `UseAirCloudKingbase` extension.

---

### 安装与引用

在需要使用 KingbaseES V9 的 Repository 或 Entry 项目中引用：

```xml
<PackageReference Include="Air.Cloud.EntityFrameWork.Kingbase" Version="6.0.0" />
```

源码开发时也可以引用项目：

```xml
<ProjectReference Include="..\..\src\db\Air.Cloud.EntityFrameWork.Kingbase\Air.Cloud.EntityFrameWork.Kingbase.csproj" />
```

---

### Provider 注册

在 EF Core 注册位置调用 `UseAirCloudKingbase`：

```csharp
using Air.Cloud.EntityFrameWork.Kingbase.Extensions;

services.AddDbContext<DefaultDbContext>(options =>
{
    options.UseAirCloudKingbase(
        Configuration["DataSourceSettings:ConnectionStrings:default"],
        "Your.Migrations.Assembly");
});
```

`UseAirCloudKingbase` 内部调用 Kingbase Provider 的 `UseKdbndp(connectionString, options => ...)`。第二个参数 `migrationAssemblyName` 可为空；为空时使用 Kingbase Provider 默认迁移程序集行为。

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
using Air.Cloud.EntityFrameWork.Core.Contexts;

public class DefaultDbContext : AppDbContext<DefaultDbContext>
{
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options)
        : base(options)
    {
    }
}
```

---

### 注意事项

- `Kdbndp.EntityFrameworkCore.KingbaseES_V9` `6.0.1.808` 是 KingbaseES V9 的 EF Core 6 系列驱动包，运行时依赖 EF Core 6 的关系型内部类型；本适配类库显式使用 EF Core `6.0.0`，避免与 EF Core 7 运行时类型不兼容。
- 当前仓库的 `Air.Cloud.EntityFrameWork.Core` 使用 EF Core `7.0.20`，因此 Kingbase 适配器暂不注册统一 `IDatabaseConfigure`/`Startup` 自动链路。若后续 Kingbase 提供 EF Core 7 驱动，或公共数据库 Core 退回 EF Core 6，可再接入统一配置器。
- 连接字符串会原样传递给 `UseKdbndp`，请按 KingbaseES V9 驱动要求配置服务器、端口、数据库、用户名和密码。
- 健康检查是否启用仅由 `ConnectionValidationSQL` 是否为空决定。需要关闭检查时，将该配置项设置为空字符串即可。
- 该适配器只负责 Provider 绑定和迁移程序集配置，不会自动创建数据库或执行迁移。
