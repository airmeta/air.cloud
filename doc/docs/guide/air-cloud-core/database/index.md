# 数据库包指南

Air.Cloud 的数据库能力分为两类：Entity Framework Core 数据访问包，以及早期占位的数据访问包。生产项目应优先使用 `Air.Cloud.EntityFrameWork.Core` 加具体数据库 Provider 包。

## 包清单

| 包 | 状态 | 作用 |
| --- | --- | --- |
| `Air.Cloud.EntityFrameWork.Core` | 维护中 | EF Core 仓储、DbContext 注册、连接配置、列元数据扩展和状态检查基础能力 |
| `Air.Cloud.EntityFrameWork.MySQL` | 维护中 | MySQL EF Core Provider 适配 |
| `Air.Cloud.EntityFrameWork.Oracle` | 维护中 | Oracle EF Core Provider 适配，并启用 Oracle 批量操作扩展 |
| `Air.Cloud.EntityFrameWork.PostgreSQL` | 维护中 | PostgreSQL EF Core Provider 适配 |
| `Air.Cloud.EntityFrameWork.Kingbase` | 维护中 | KingbaseES V9 Provider |

## 标准接入流程

1. 引用 `Air.Cloud.EntityFrameWork.Core`。
2. 按数据库类型引用一个 Provider 包，例如 `Air.Cloud.EntityFrameWork.MySQL`。
3. 在配置中写入 `DataSourceSettings`。
4. 在业务启动中调用 `AddDb<TDbContext>()` 或 `AddDbPool<TDbContext>()`。

```json
{
  "DataSourceSettings": {
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

`DataSourceSettings:ConnectionStrings` 是 Air.Cloud 的正式命名连接字符串节点。`connectionMetadata` 新调用应传入该节点下的名称，例如 `default`；解析后的结果始终是一个原始连接字符串，再交给具体 EF Core Provider。官方根节点 `ConnectionStrings`、完整配置路径和直接传入连接字符串仅保留用于兼容历史调用，不应作为新配置格式。

环境变量应使用配置节点的标准映射，例如：

```text
DataSourceSettings__ConnectionStrings__default=Server=...;Database=...;
```

## Provider 选择

一个应用通常只引入一个关系型数据库 Provider 包。多个 Provider 包都会注册 `IDatabaseConfigure`，如果同一个应用同时引入多个 Provider，需要明确处理注册覆盖关系，否则最后生效的实现可能不符合预期。

## 相关文档

- [EF Core 核心包](./efcore-core.md)
- [EF Core 事务边界](./transactions.md)
- [MySQL Provider](./mysql.md)
- [Oracle Provider](./oracle.md)
- [PostgreSQL Provider](./postgresql.md)
- [Kingbase Provider](./kingbase.md)
- [列元数据提供器](./column-metadata-provider.md)
