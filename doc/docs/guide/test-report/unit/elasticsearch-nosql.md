# ElasticSearch NoSQL 单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/ElasticSearch`。

目标是验证 `INoSqlRepository<T>` 仓储契约、连接池行为和索引命名规则。单元测试使用内存仓储，不连接真实 ES 集群。

## 2. 测试内容与边界

### 仓储 CRUD 边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 保存文档 | `Id = doc-1`，`Tags = integration,created`，`Url = /integration/es/create` | 无 | `SaveAsync` 返回同 Id 文档。 |
| 按 Id 查询 | 查询 `doc-1` | 文档已保存 | 返回文档非空，Url 与写入值一致。 |
| 更新文档 | `Tags = integration,updated`，`Url = /integration/es/update` | 文档已存在 | `UpdateAsync` 返回同 Id，查询结果包含更新后的 Tags。 |
| 删除文档 | 删除 `doc-1` | 文档已存在 | 删除返回 `true`，再次查询返回 `null`。 |

### 批量与缺失文档边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 批量保存 | `batch-1`、`batch-2`，`Tags = batch` | 多文档输入 | 保存返回 `true`，按 `Tags = batch` 查询数量为 2。 |
| 查询不存在文档 | `Id = missing-document` | 文档未写入 | 返回 `null`，不抛异常。 |
| 删除不存在文档 | `Id = missing-document` | 文档未写入 | 抛出 `InvalidOperationException`，避免误判删除成功。 |
| 更新不存在文档 | `Id = missing-document` | 文档未写入 | 抛出 `InvalidOperationException`，避免静默创建错误数据。 |

### 连接与索引命名边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 连接池重建 | 清理连接池后再次创建仓储 | 池中无连接 | 仓储可重新建立连接。 |
| 年切分索引 | `Name = air-cloud-it`，`Pattern = Year` | 当前年份 | 生成 `air-cloud-it-{yyyy}`。 |
| 月切分索引 | `Name = air-cloud-it`，`Pattern = Month` | 当前年月 | 生成 `air-cloud-it-{yy}-{M}`。 |
| 日切分索引 | `Name = air-cloud-it`，`Pattern = Day` | 当前年内天数 | 生成 `air-cloud-it-{yy}-{DayOfYear}`。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`6/12` 通过，`6` 失败。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~Air.Cloud.UnitTest.Modules.ElasticSearch"
```

失败用例集中在 `ElasticSearchTests.cs`，均为 `10s` 超时：

- `Save_should_recreate_connection_after_pool_is_cleared`
- `SaveAsync_should_return_saved_document_with_same_identifier`
- `Update_should_persist_modified_document_values`
- `Delete_should_remove_saved_document`
- `Query_should_return_saved_document`
- `Save_should_persist_trace_log_document`

说明：`ElasticSearchMiddlewareContractTests` 契约类通过；失败的是旧的真实连接/连接池相关单元类。当前报告按完整模块过滤口径记录失败，不再用混合通过结果替代。
## 4. 对应目标中间件版本

单元测试不连接真实 ElasticSearch 集群。目标是当前源码中的 NoSQL 仓储契约与索引命名规则。

