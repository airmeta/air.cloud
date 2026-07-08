# FreeSql 与 SqlLite 包

`Air.Cloud.DataBase.FreeSql` 和 `Air.Cloud.DataBase.SqlLite` 当前是早期占位包。

## 当前状态

| 包 | TargetFramework | 当前能力 |
| --- | --- | --- |
| `Air.Cloud.DataBase.FreeSql` | `net6.0` | 没有公开 Air.Cloud 注册扩展或标准实现 |
| `Air.Cloud.DataBase.SqlLite` | `net6.0` | 仅包含默认占位类，没有 EF Core/Sqlite 接入封装 |

## 使用建议

- 新项目不要优先选择这两个包作为数据库接入入口。
- 如果需要 SQLite 的 EF Core 支持，当前应在业务项目中直接使用 EF Core 的 `UseSqlite(...)`，或新增正式的 `Air.Cloud.EntityFrameWork.Sqlite` Provider 包。
- 如果需要 FreeSql，建议先补齐明确的标准接口、配置节点、生命周期和测试，再作为正式模块发布。

## 与 EF Core 包的区别

`Air.Cloud.EntityFrameWork.*` 系列已经接入 `IDatabaseConfigure`、`DataSourceSettings`、`AddDb` / `AddDbPool`、仓储和状态检查链路。FreeSql/SqlLite 占位包当前没有这些集成能力。
