# Air.Cloud Micro API 示例

这是 Air.Cloud 的后端微服务示例，包含 Open 服务和安全域服务。项目通过 NuGet 引用 `Air.Cloud.*` 包，用于验证消费者的真实集成方式，而不是直接引用本仓库的 `src` 源码。

前端未随本示例迁入。后续管理端基于 `demo/admin/VbenAdmin` 维护。

## 结构

```text
Open/      开放服务
Security/  Account、Auth、System 服务与公共库
```

`Air.Cloud.Micro.Api.slnx` 只包含后端 API 示例项目，未加入根目录的 `air.net.cloud.sln`，避免示例的外部运行依赖影响框架默认构建。

Gateway 产品源码已迁移到 `demo/airGateway`，不再放在 `demo/api` 中。

## 配置

仓库只提交 `appsettings.Example.json` 和 `appsettings.Development.Example.json`。在每个 Entry 项目目录中复制对应文件并命名为 `appsettings.json` 或 `appsettings.Development.json`，再填写本地环境地址和凭据。

示例服务使用 Consul、Redis 和 PostgreSQL。System、Auth、Account 使用 `WebInjectInConsul()`，其完整业务配置应在本地 Consul 中维护；Open 服务使用本地文件配置。不要将连接字符串、令牌或私钥提交到仓库。

## 构建

```powershell
dotnet build .\Air.Cloud.Micro.Api.slnx
```

## 数据库初始化

[`database/postgresql/init.sql`](database/postgresql/init.sql) 包含 demo 表结构的 PostgreSQL DDL，不包含凭据或业务数据。[`database/postgresql/seed.sql`](database/postgresql/seed.sql) 包含转换后的测试默认数据。先创建目标数据库，再按顺序导入结构和数据。

```powershell
createdb -h localhost -p 5432 -U <user> air_cloud_demo
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\database\postgresql\init.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\database\postgresql\seed.sql
```

导入后，将 `OpenConnectionString` 和 `SecurityConnectionString` 指向该 PostgreSQL 数据库。测试数据仅用于本地或隔离环境，不应用于生产环境。

先准备上述运行依赖和本地配置，再按需启动以下 Entry 项目：

```text
Open/air.cloud.open.entry
Security/Auth/air.cloud.auth.entry
Security/Account/air.cloud.account.entry
Security/System/air.cloud.system.entry
```
