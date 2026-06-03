# 安全与插件扩展标准

安全与插件扩展标准用于承载鉴权、接口文档、API 目录等 Web/API 增强能力。它们通常不属于服务运行的硬依赖，但会影响接口访问、调试和治理体验。

## 标准接口与插件能力

| 能力 | 作用 |
| --- | --- |
| `IJwtHandlerStandard` | JWT 处理标准入口 |
| `IAppPluginFactory` | 插件注册与获取工厂 |
| `IPlugin` | 插件基础标记接口 |
| `Air.Cloud.Plugins.Jwt` | JWT 鉴权插件 |
| `Air.Cloud.Plugins.SpecificationDocument` | Swagger / OpenAPI 文档插件 |
| `Air.Cloud.Plugins.APICatalog` | API 目录与探针插件 |
| `Air.Cloud.Plugins.CodeGenerator` | 代码生成插件 |
| `Air.Cloud.Plugins.Security` | 安全辅助插件 |

## 使用建议

- 插件不要放复杂业务流程，复杂业务应放在业务服务或模块里。
- 鉴权和文档属于 Web/API 横切能力，建议在应用启动阶段统一注册。
- 如果业务需要替换默认插件，应在启动阶段调用 `AppRealization.SetPlugin()`。

## 实现文档

- JWT：见 `插件` 下的 `JWT 鉴权`。
- Swagger：见 `插件` 下的 `接口文档`。
