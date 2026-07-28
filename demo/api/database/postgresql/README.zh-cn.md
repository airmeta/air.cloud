# PostgreSQL 初始化

`init.sql` 是 demo 数据库的 PostgreSQL DDL，包含安全域和开放平台共用的表结构，不包含连接凭据。`seed.sql` 是从原 demo 数据转换后的 PostgreSQL 测试数据，应仅用于本地或隔离环境。

先创建目标数据库，再进入目标库执行脚本：

```powershell
createdb -h localhost -p 5432 -U <user> air_cloud_demo
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\init.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\seed.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\migrate-system-menus-to-vben.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\copy-default-user-system-role-to-vben.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\migrate-open-app-management-to-system.sql
```

执行顺序必须是 `init.sql` 在前、`seed.sql` 在后。随后将 `OpenConnectionString` 和 `SecurityConnectionString` 指向同一个 PostgreSQL 数据库。

如果既有数据库中已经存在 `timestamp with time zone` 字段，先执行 `alter-timestamps-without-time-zone.sql` 将时间字段转换为 `timestamp without time zone`，避免 Npgsql 写入本地 `DateTime` 时报错。

`migrate-system-menus-to-vben.sql` 将旧系统菜单的路径、组件和图标更新为 Vben 格式；`apps/system` 不再兼容旧的 `@room/views` 或单数路由值。

`copy-default-user-system-role-to-vben.sql` 将默认账户 `6541de08432942c489460878be58d1c2` 的管理员角色和有效菜单权限复制到 Vben System AppId `4841de0858714c5e8bee36e2fad70614`。该脚本还会创建 Vben 隐藏详情路由 `/system/roles/authorize/:id`，保证角色授权页能通过后端动态路由进入但不显示在菜单中。该脚本必须在菜单迁移脚本之后执行，并且可重复运行。

`migrate-open-app-management-to-system.sql` 将接口开放平台中重复的应用管理和路由管理菜单下线，并在 Vben System 菜单树中创建路由管理菜单及默认管理员角色关联。该脚本必须在 `copy-default-user-system-role-to-vben.sql` 之后执行。
