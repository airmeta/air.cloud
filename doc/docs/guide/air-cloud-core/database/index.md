# 数据库包指南

Air.Cloud 的数据库能力分为两类：Entity Framework Core 数据访问包，以及早期占位的数据访问包。生产项目应优先使用 `Air.Cloud.EntityFrameWork.Core` 加具体数据库 Provider 包。

## 包清单

| 包 | 状态 | 作用 |
| --- | --- | --- |
| `Air.Cloud.EntityFrameWork.Core` | 推荐 | EF Core 仓储、DbContext 注册、连接配置、列元数据扩展和状态检查基础能力 |
| `Air.Cloud.EntityFrameWork.MySQL` | 推荐 | MySQL EF Core Provider 适配 |
| `Air.Cloud.EntityFrameWork.Oracle` | 推荐 | Oracle EF Core Provider 适配，并启用 Oracle 批量操作扩展 |
| `Air.Cloud.EntityFrameWork.PostgreSQL` | 推荐 | PostgreSQL EF Core Provider 适配 |
| `Air.Cloud.EntityFrameWork.Kingbase` | 推荐 | KingbaseES V9 EF Core Provider 适配 |

## 标准接入流程

1. 引用 `Air.Cloud.EntityFrameWork.Core`。
2. 按数据库类型引用一个 Provider 包，例如 `Air.Cloud.EntityFrameWork.MySQL`。
3. 在配置中写入 `DataSourceSettings`。
4. 在业务启动中调用 `AddDb<TDbContext>()` 或 `AddDbPool<TDbContext>()`。

```json
{
  "DataSourceSettings": {
    "ConnectionValidationSQL": "select 1",
    "ConnectionValidationIntervalMillis": 60000,
    "ConnectionStrings": {
      "default": "your-connection-string"
    }
  }
}
```

```csharp
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

services.AddDb<DefaultDbContext>(connectionMetadata: "default");
```

`connectionMetadata` 可以是连接字符串、`DataSourceSettings:ConnectionStrings` 中的 key，或完整配置路径。具体解析逻辑由 `DbProvider.GetConnectionString<TDbContext>()` 处理。

## Provider 选择

一个应用通常只引入一个关系型数据库 Provider 包。多个 Provider 包都会注册 `IDatabaseConfigure`，如果同一个应用同时引入多个 Provider，需要明确处理注册覆盖关系，否则最后生效的实现可能不符合预期。

## 相关文档

- [EF Core 核心包](./efcore-core.md)
- [MySQL Provider](./mysql.md)
- [Oracle Provider](./oracle.md)
- [PostgreSQL Provider](./postgresql.md)
- [Kingbase Provider](./kingbase.md)
- [列元数据提供器](./column-metadata-provider.md)
