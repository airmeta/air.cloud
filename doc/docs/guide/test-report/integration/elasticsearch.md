# ElasticSearch 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/ElasticSearch/ElasticSearchRealClusterIntegrationTests.cs`。

目标是连接真实 ElasticSearch 集群，验证 NoSQL 仓储实现、批量写入、缺失文档行为和索引命名规则。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `ElasticSearchIntegration:RunElasticSearchTests` | `true` |
| `ElasticSearchIntegration:ConnectionString` | `http://192.168.100.31:9200/` |
| `ElasticSearchIntegration:UserName` | 空 |
| `ElasticSearchIntegration:Password` | 空 |
| `ElasticSearchIntegration:IndexName` | `air-cloud-it` |

## 3. 测试内容与边界

### 文档 CRUD

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 保存文档 | `Id = Guid`，`Tags = integration,created`，`Url = /integration/es/create` | 无 | `SaveAsync` 返回同 Id。 |
| 按 Id 查询 | 查询刚保存的 Id | 索引刷新后读取 | 返回文档非空，Url 与写入值一致。 |
| 更新文档 | `Tags = integration,updated`，`Url = /integration/es/update` | 文档已存在 | `UpdateAsync` 返回同 Id，刷新后可查询到更新文档。 |
| Query 查询 | Term Query：`field = Id`，`value = document.Id` | 精确 Id 查询 | 查询结果包含目标文档。 |
| 删除文档 | 删除刚写入的 Id | 删除后再次读取 | 最终 `FirstOrDefaultAsync` 返回 `null`。 |

### 批量写入

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 批量保存 | 两个文档：`Tags = batch`，Url 分别为 `/batch/1`、`/batch/2` | 多文档输入 | `SaveAsync(IEnumerable)` 返回 `true`。 |
| Ids Query | 使用两个刚写入文档的 Id 数组 | 多 Id 查询 | 查询数量为 2，包含两个目标 Id。 |
| 清理数据 | finally 中逐个删除批量文档 | 清理失败 | 清理异常不覆盖前置断言结果。 |

### 缺失文档边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 查询不存在文档 | `Id = missing-{Guid}` | 文档未写入 | 返回 `null`，不抛异常。 |
| 删除不存在文档 | `DeleteAsync("missing-{Guid}")` | 文档未写入 | 抛出异常，避免调用方误判删除成功。 |
| 更新不存在文档 | `Id = missing-{Guid}`，`Tags = missing`，`Url = /integration/es/missing` | 文档未写入 | 抛出异常，避免静默创建错误数据。 |

### 索引命名边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 年切分 | `Name = air-cloud-it`，`Pattern = Year` | 当前年份 | 生成 `air-cloud-it-{yyyy}`。 |
| 月切分 | `Name = air-cloud-it`，`Pattern = Month` | 当前月份 | 生成 `air-cloud-it-{yy}-{M}`。 |
| 日切分 | `Name = air-cloud-it`，`Pattern = Day` | 当前年内第几天 | 生成 `air-cloud-it-{yy}-{DayOfYear}`。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`6/6` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "Module=ElasticSearch"
```
## 5. 对应目标中间件版本

目标 ElasticSearch 集群为 `http://192.168.100.31:9200/`。当前配置未声明 ES 服务端版本，报告不臆测具体版本。

