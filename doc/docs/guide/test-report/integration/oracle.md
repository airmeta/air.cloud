# Oracle 只读集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/DataBase/OracleReadOnlyIntegrationTests.cs`。

目标是通过真实 Oracle 连接执行只读查询，用于验证数据库访问链路。当前默认关闭。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `RealDbIntegration:RunReadOnlyOracleQuery` | `false` |
| `RealDbIntegration:OracleConnectionStringName` | `OracleConnectionString` |
| `ConnectionStrings:OracleConnectionString` | 空 |
| `RealDbIntegration:OracleQueryUserId` | `a09cdb089b7f48498090d1f7f11c0e7b` |

## 3. 测试内容与边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 开关关闭 | `RunReadOnlyOracleQuery = false` | 未提供连接字符串 | 测试直接返回，不访问 Oracle。 |
| 只读查询 | 开关开启，连接字符串有效，`OracleQueryUserId` 有值 | 连接字符串为空或目标用户不存在 | 有效环境下返回查询结果；无效环境应作为集成环境问题处理。 |
| 安全边界 | 只读查询 | 写入/更新/删除操作 | 测试不执行写操作，避免污染真实数据库。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`1/1` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "FullyQualifiedName~OracleReadOnlyIntegrationTests"
```

注意：当前 `RealDbIntegration:RunReadOnlyOracleQuery = false`，所以该结果只证明关闭开关时的跳过路径正常，不代表真实 Oracle 查询已经通过。
## 5. 对应目标中间件版本

当前配置未声明 Oracle 服务端版本，报告不臆测具体版本。启用前需要补充真实连接字符串和目标环境说明。

