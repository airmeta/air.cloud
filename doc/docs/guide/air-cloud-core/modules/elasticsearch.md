# ElasticSearch

`Air.Cloud.Modules.ElasticSearch` 是 Air.Cloud 的 NoSQL 数据访问模块，基于 `NEST 7.10.0` / `Elasticsearch.Net 7.10.0` 注册 Elasticsearch 仓储实现。

## 所属 Standard

| Standard | 模块实现 | 说明 |
| --- | --- | --- |
| `INoSqlRepository<T>` | `ESNoSqlRepository<T>` | Elasticsearch 文档仓储实现 |
| `INoSqlEntity` | 业务文档实体实现 | 文档实体标准 |

## 包名

```text
Air.Cloud.Modules.ElasticSearch
```

## 加载机制

模块 `Startup` 会绑定数据库配置并注册 Elasticsearch 仓储：

```csharp
services.AddOptions<DataBaseOptions>()
    .BindConfiguration("DataBaseSettings")
    .ValidateDataAnnotations();

services.AddElasticSearch();
```

`AddElasticSearch()` 会扫描 `AppCore.Assemblies`，找到同时满足以下条件的类型：

- 是 class。
- 非 abstract。
- 实现 `INoSqlEntity`。
- 标记了 `ElasticSearchIndexAttribute`。

满足条件的实体会被注册为：

```csharp
INoSqlRepository<TDocument> -> ESNoSqlRepository<TDocument>
```

## 配置节点

配置节点名称为：

```text
DataBaseSettings
```

Elasticsearch 模块从 `DataBaseSettings:Options` 中按 `ElasticSearchIndexAttribute.DbKey` 查找连接配置。

| 配置项 | 说明 |
| --- | --- |
| `Options[].Key` | 连接配置名称，需要与实体上的 `DbKey` 一致 |
| `Options[].ConnectionString` | Elasticsearch 节点地址，多个节点用英文逗号分隔 |
| `Options[].Account` | Basic Auth 用户名，可为空 |
| `Options[].Password` | Basic Auth 密码，可为空 |
| `Options[].Type` | 建议配置为 `NOSQL`，用于表达该连接为非关系型数据库 |

## 配置示例

```json
{
  "DataBaseSettings": {
    "Options": [
      {
        "Key": "DefaultElasticSearch",
        "Type": "NOSQL",
        "ConnectionString": "http://192.168.100.156:9200",
        "Account": "",
        "Password": ""
      }
    ]
  }
}
```

多个节点：

```json
{
  "DataBaseSettings": {
    "Options": [
      {
        "Key": "DefaultElasticSearch",
        "Type": "NOSQL",
        "ConnectionString": "http://192.168.100.156:9200,http://192.168.100.157:9200"
      }
    ]
  }
}
```

## 定义文档实体

实体必须实现 `INoSqlEntity`，并标记 `ElasticSearchIndexAttribute`：

```csharp
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Enums;

[ElasticSearchIndex(
    DbKey = "DefaultElasticSearch",
    TableName = "order_trace",
    SegmentationPattern = IndexSegmentationPatternEnum.Month,
    SegmentationTag = "-")]
public sealed class OrderTraceDocument : INoSqlEntity
{
    public string Id { get; set; }

    public string OrderId { get; set; }

    public string Status { get; set; }

    public DateTime CreatedTime { get; set; }
}
```

属性说明：

| 属性 | 说明 |
| --- | --- |
| `DbKey` | 对应 `DataBaseSettings:Options[].Key` |
| `TableName` | Elasticsearch 索引基础名 |
| `SegmentationPattern` | 索引切分规则 |
| `SegmentationTag` | 索引名分隔符，默认 `-` |

## 索引切分规则

`IndexSegmentationPatternEnum` 支持：

| 值 | 索引名规则 |
| --- | --- |
| `None` | `TableName` |
| `Year` | `TableName-Year` |
| `Month` | `TableName-(Year - 2000)-Month` |
| `Day` | `TableName-(Year - 2000)-DayOfYear` |

示例：`TableName = "order_trace"`，当前时间为 2026 年 6 月，`Month` 模式下索引名类似：

```text
order_trace-26-6
```

## 使用仓储

通过 DI 解析 `INoSqlRepository<TDocument>`：

```csharp
public sealed class OrderTraceService
{
    private readonly INoSqlRepository<OrderTraceDocument> _repository;

    public OrderTraceService(INoSqlRepository<OrderTraceDocument> repository)
    {
        _repository = repository;
    }

    public async Task SaveAsync(OrderTraceDocument document)
    {
        await _repository.SaveAsync(document);
    }

    public async Task<OrderTraceDocument> GetAsync(string id)
    {
        return await _repository.FirstOrDefaultAsync(id);
    }
}
```

常用方法：

| 方法 | 说明 |
| --- | --- |
| `Save()` / `SaveAsync()` | 保存单个文档 |
| `Save(IEnumerable<T>)` / `SaveAsync(IEnumerable<T>)` | 批量保存文档 |
| `Update()` / `UpdateAsync()` | 更新文档 |
| `FirstOrDefault()` / `FirstOrDefaultAsync()` | 按 Id 查询文档 |
| `Delete()` / `DeleteAsync()` | 按 Id 删除文档 |
| `Client<TClient>()` | 获取底层客户端，例如 `IElasticClient` |

## 使用原生 IElasticClient

需要复杂查询时，可以获取底层 `IElasticClient`：

```csharp
var client = _repository.Client<IElasticClient>();

var response = await client.SearchAsync<OrderTraceDocument>(s => s
    .Query(q => q
        .Term(t => t.Field(f => f.OrderId).Value("SO202606030001"))));
```

## 注意事项

- `DbKey` 必须能在 `DataBaseSettings:Options` 中找到对应配置。
- `ConnectionString` 不能为空，多个节点用英文逗号分隔。
- 当前模块使用 `SniffingConnectionPool`，节点地址必须是应用可访问地址。
- 实体必须实现 `INoSqlEntity` 并包含 `Id`。
- 实体必须带 `ElasticSearchIndexAttribute`，否则不会注册仓储。
- 仓储注册依赖 `AppCore.Assemblies` 扫描结果，实体所在程序集必须被框架加载。
- 当前模块基于 `NEST 7.10.0`，更高版本 Elasticsearch 集群兼容性需要单独验证。
