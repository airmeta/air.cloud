###  数据源配置

配置类: DataSourceOptions

命名空间: `Air.Cloud.DataBase.Options`

短述（zh-cn）: 数据源的通用配置项，包含连接校验、校验间隔以及多命名连接字符串的管理。

Short (en-us): Common data source options including connection validation, validation interval, and named connection strings management.

---

### 构造函数

- 默认构造函数：`DataSourceOptions()`

说明：无特殊初始化逻辑

---

### 属性（Properties）

| 名称 | 类型 | 说明 | 默认值 |
|---|---|---|---|
| `ConnectionValidationSQL` | `string` | 连接验证 SQL 语句；用于探活/健康检查或连接可用性验证。 | `string.Empty` |
| `ConnectionValidationIntervalMillis` | `int` | 连接验证间隔时间（毫秒）。 | `60000`（1 分钟） |
| `ConnectionStrings` | `IDictionary<string, string>` | 数据库连接字符串集合，键为名称，值为连接串。 | `null` |

---

### 方法（Methods）

- `string GetConnectionString(string name)`
  - 作用：获取指定名称的连接字符串。
  - 返回：存在则返回对应连接字符串；不存在返回空字符串 `""`。

- `string SetConnectionString(string name, string connectionString)`
  - 作用：设置/更新指定名称的连接字符串；如 `ConnectionStrings` 为空，将自动创建 `Dictionary<string,string>`。
  - 返回：入参 `connectionString` 原样返回，便于链式/内联使用。

- `bool EnableDatabaseStatusCheck()`
  - 作用：是否启用数据库状态检查。
  - 判定规则：当且仅当 `ConnectionValidationSQL` 非空时返回 `true`，否则返回 `false`。

---

### 使用示例（C#）

```csharp
using Air.Cloud.DataBase.Options;

var options = new DataSourceOptions
{
    ConnectionValidationSQL = "连接验证语句",
    ConnectionValidationIntervalMillis = 60000
};

// 设置/更新命名连接串
options.SetConnectionString("default", "Server=...;Database=...;User Id=...;Password=...;");
options.SetConnectionString("reporting", "Server=...;Database=...;Trusted_Connection=True;");

// 读取连接串
var defaultConn = options.GetConnectionString("default"); // 若不存在则为 ""

// 判断是否启用数据库状态检查
bool enabled = options.EnableDatabaseStatusCheck(); // ConnectionValidationSQL 非空即 true
```

---

### 备注（Notes）

- 若仅需要关闭健康检查，将 `ConnectionValidationSQL` 置空即可。
- 建议为不同用途（读写分离、报表、批处理）使用不同的命名连接串，便于按需获取与管理。

---