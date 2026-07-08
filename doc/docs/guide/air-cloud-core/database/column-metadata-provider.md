### EF Core 列元数据提供器

`Air.Cloud.EntityFrameWork.Core` 提供列元数据扩展点，用于在 `DbContext.OnModelCreating` 阶段按实体属性动态修改 EF Core 关系型元数据，例如列类型、列名、精度或长度。

Short (en-us): `Air.Cloud.EntityFrameWork.Core` provides a column metadata extension point that runs during `DbContext.OnModelCreating` and can adjust EF Core relational metadata such as column type, column name, precision, or length for each entity property.

---

### 适用场景

- Kingbase 项目需要把 `DateTime` 明确映射为 `timestamp without time zone`。
- Oracle 或其他数据库需要集中处理 `string`、`decimal`、`DateTime` 等 CLR 类型的数据库类型。
- 业务项目希望通过特性覆盖个别字段的类型，而不是为每个实体编写完整的 `IEntityTypeBuilder<TEntity>`。

该扩展点只在 EF Core 模型构建阶段执行。模型已经缓存后，运行时修改注册或规则不会自动刷新；需要动态刷新模型时，应配合 `DbContextMode.Dynamic` 使用。

---

### 注册方式

框架不会自动注册默认实现，数据库模块也不会默认启用列元数据改写。业务项目需要显式注册自己的实现：

```csharp
services.AddSingleton<IColumnMetadataProvider, MyColumnMetadataProvider>();
```

`DefaultColumnMetadataProvider` 是可继承的基础类，不会自动生效。

---

### 类型映射示例

```csharp
using Air.Cloud.EntityFrameWork.Core.Entities.Configures;

public sealed class MyColumnMetadataProvider : DefaultColumnMetadataProvider
{
    protected override IReadOnlyDictionary<Type, string> TypeMappings { get; } = new Dictionary<Type, string>
    {
        [typeof(DateTime)] = "timestamp without time zone",
        [typeof(string)] = "varchar(255)",
        [typeof(decimal)] = "numeric(18,2)"
    };
}
```

`DateTime?` 会按 `DateTime` 的映射处理，因此不需要为可空值类型重复配置。

---

### 属性级覆盖

可以使用 `ColumnTypeAttribute` 覆盖类型映射表：

```csharp
using Air.Cloud.EntityFrameWork.Core.Entities.Attributes;

public sealed class OrderEntity
{
    public DateTime CreatedAt { get; set; }

    [ColumnType(typeof(string), "varchar(64)")]
    public string? OrderNo { get; set; }
}
```

默认提供器先应用 `TypeMappings`，再应用 `ColumnTypeAttribute`，所以属性级声明拥有最终列类型。

如果只想声明该字段按公共类型映射处理，可以只传 CLR 类型：

```csharp
[ColumnType(typeof(string))]
public string? CommonCode { get; set; }
```

这会先校验属性类型是 `string`，然后使用 `TypeMappings[typeof(string)]`。如果同时传入数据库类型名称，则使用特性上的类型名称强制覆盖公共映射：

```csharp
[ColumnType(typeof(string), "varchar(64)")]
public string? OrderNo { get; set; }
```

---

### 自定义 Apply

需要改列名或其他 EF Core 元数据时，可以重写 `Apply`：

```csharp
public sealed class MyColumnMetadataProvider : DefaultColumnMetadataProvider
{
    protected override IReadOnlyDictionary<Type, string> TypeMappings { get; } = new Dictionary<Type, string>
    {
        [typeof(DateTime)] = "timestamp without time zone"
    };

    public override void Apply(ColumnMetadataContext context)
    {
        base.Apply(context);

        var columnName = ToSnakeCase(context.PropertyInfo.Name);
        context.Property.SetColumnName(columnName);
    }
}
```

`ColumnMetadataContext` 会提供当前的 `ModelBuilder`、`EntityTypeBuilder`、实体元数据、属性元数据、`PropertyInfo`、`DbContext` 和 DbContext 定位器。

---

### 注意事项

- 未注册 `IColumnMetadataProvider` 时，框架不会改变任何列元数据。
- 多次调用 `SetColumnType` 时，后一次会覆盖前一次。
- `ColumnTypeAttribute` 的 `typeof(...)` 必须与属性类型匹配；可空值类型按基础类型匹配。
- 该扩展点面向模型元数据，不负责运行时值转换。
