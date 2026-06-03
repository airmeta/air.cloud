# WebApp 与 APICatalog 单元测试报告

## 1. 测试范围

覆盖 `Air.Cloud.WebApp` 的动态 API、统一返回、友好异常、数据验证，以及 `Air.Cloud.Plugins.APICatalog` 的 APIProbe Provider 与中间件行为。

## 2. 本次重点变更

本轮重点验证 DynamicApi 与 APICatalog 的标准元数据链路：

- DynamicApi 内部职责拆分后，动词、命名、HTTP 方法、参数绑定、路由、统一返回元数据仍保持原行为。
- DynamicApi 组件通过 DI 注册并由 convention 组合，避免把 resolver/builder 固化在 convention 内部。
- `ApiDescriptionSettingsAttribute` 会生成 `APIProbeEndpointMetadata` 并写入 MVC `EndpointMetadata`。
- `Penetrates.ControllerOrderCollection` 不再由 DynamicApi convention 写入，避免全局状态污染。
- APICatalog 默认 `ApiExplorerAPIProbeProvider` 读取 `APIProbeEndpointMetadata`，并按 `Group -> Order -> Path -> Method` 排序。

## 3. 测试内容与边界

| 类别 | 覆盖点 | 关键边界 | 预期结果 |
| --- | --- | --- | --- |
| 动态 API 动词映射 | 默认动词、自定义动词、双词动词 | `getlist`、自定义 verb、多 convention 实例 | 正确推断 HTTP 方法；不污染全局动词表。 |
| 动态 API 命名 | 控制器 / Action affix 裁剪、`KeepVerb` | `Async` 后缀、动词保留、小写路由 | ActionName 与路由生成稳定。 |
| 参数绑定 | Body、Query、Route 参数 | `ModelToQuery`、`QueryParametersAttribute`、数组、URL 参数化 | 绑定源符合设计规则。 |
| 路由生成 | 参数 seat、约束、大小写 | ControllerStart、ControllerEnd、ActionEnd、`:guid` | 路由模板稳定生成。 |
| 统一返回元数据 | bool、void、已包装模型 | `RESTfulResult<bool>`、`void` | bool 可生成返回模型；void 不生成成功包装元数据。 |
| APIProbe 元数据 | 控制器级、方法级、空配置 | 方法级覆盖控制器级；无配置不写入 | 只在需要时写入 `APIProbeEndpointMetadata`。 |
| APICatalog 读取 | 标准元数据读取、Fallback、分组过滤、排序 | 缺少标准元数据、负数 Order、默认 Order | 输出分组、标签、描述和排序符合标准。 |
| 注册顺序 | WebApp / UnifyResult 组合 | 反向注册、只注册 Core、只注册 UnifyResult | MVC convention 和过滤器顺序稳定。 |
| 友好异常 | Oops、IfException、异常包装 | 业务异常、全局处理器、统一返回 | 异常结果可被统一返回 Provider 包装。 |

## 4. 本轮新增测试

本轮在已有 WebApp / APICatalog 测试基础上继续补充：

- `DynamicApiConvention_should_prefer_action_api_probe_metadata_over_controller_metadata`
- `DynamicApiConvention_should_not_write_api_probe_metadata_when_description_settings_absent`
- `ApiExplorer_provider_should_filter_by_standard_metadata_group_name`
- `ApiExplorer_provider_should_fallback_when_standard_metadata_is_missing`
- `ApiExplorer_provider_should_keep_zero_order_as_default`

加上上一轮已补充的 DI 注册、无全局排序状态、APICatalog 标准元数据消费等用例，DynamicApi/APICatalog 元数据链路已有正向、覆盖、空配置、Fallback、排序和过滤边界。

## 5. 测试结果

本报告已单独运行 WebApp、APICatalog、FriendlyException 相关过滤命令，结果：`105/105` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~WebApp|FullyQualifiedName~APICatalog|FullyQualifiedName~FriendlyException"
```

## 6. 构建结果

相关类库构建结果均为 `0` 警告、`0` 错误：

```bash
dotnet build src/core/Air.Cloud.Core/Air.Cloud.Core.csproj --no-restore
dotnet build src/core/Air.Cloud.WebApp/Air.Cloud.WebApp.csproj --no-restore
dotnet build src/plugins/Air.Cloud.Plugins.APICatalog/Air.Cloud.Plugins.APICatalog.csproj --no-restore
```

## 7. 结论

DynamicApi 与 APICatalog 的元数据链路已经从全局状态迁移到 MVC `EndpointMetadata` 标准承载方式。  
当前测试覆盖可以防止以下回归：

- 动态 API convention 重新写入 `ControllerOrderCollection`。
- resolver/builder 重新退回 convention 内部硬编码。
- APICatalog 忽略 `APIProbeEndpointMetadata.Order`。
- 方法级 `ApiDescriptionSettingsAttribute` 覆盖规则失效。
- 缺少标准元数据时 APICatalog Fallback 失效。
