# Web服务总览

`Air.Cloud.WebApp` 用于快速构建 Web API 服务。它把常见 Web 能力拆成了几块：动态 API、数据验证、友好异常、统一返回、短路状态码处理、自定义返回结构。

这份文档只按使用者最关心的顺序组织：

1. 什么时候用。
2. 怎么创建 Web 服务。
3. 怎么配置统一返回。
4. 怎么配置数据验证。
5. 怎么配置异常处理。
6. 哪些地方可以自定义重写。

## 应用场景

### 适合使用 WebApp 的场景

适合：

- 需要快速创建 Web API 服务。
- 需要动态 API 能力，避免手写大量 Controller。
- 需要统一处理参数验证失败。
- 需要统一处理业务异常和系统异常。
- 需要统一 API 返回结构。
- 希望前端可以稳定解析验证错误字段。

不一定适合：

- 纯 MVC 页面项目。
- 大量文件下载、重定向、HTML 页面返回的项目。
- 已经有完整自定义 Web 框架封装的老项目。

如果只是 Web API 服务，推荐直接使用 `AddWebApp()`。  
如果是老项目迁移，推荐先使用 `AddWebAppCore()`，确认路由、验证、异常都正常后，再启用统一返回。

### 能力拆分

| 入口 | 包含能力 | 说明 |
| --- | --- | --- |
| `AddWebAppCore()` | 动态 API、数据验证、友好异常 | Web 服务核心能力，不包装成功返回 |
| `AddWebAppUnifyResult()` | 统一返回 | 只启用统一返回能力 |
| `AddWebApp()` | Core + 统一返回 | 新项目推荐入口 |

旧入口 `AddInject()` 和 `AddInjectWithUnifyResult()` 已移除。

新代码必须使用 `AddWebAppCore()`、`AddWebAppUnifyResult()` 或 `AddWebApp()`。如果历史项目仍在调用旧入口，需要按下面规则直接替换：

| 已移除入口 | 替换为 |
| --- | --- |
| `AddInject()` | `AddWebAppCore()` |
| `AddInjectWithUnifyResult()` | `AddWebApp()` |

---

## 子级文档

- [创建 Web 服务](./getting-started.md)：说明 `AddWebApp()`、`AddWebAppCore()` 和拆开注册方式。
- [统一返回配置](./unify-result.md)：说明默认返回结构、状态码配置和短路状态码处理中间件。
- [DataValidation 数据验证](./data-validation.md)：说明全局模型验证、手动验证 API 和验证失败返回结构。
- [FriendlyException 友好异常](./friendly-exception.md)：说明 `Oops` 使用方式、业务异常、普通友好异常和错误码。
- [异常处理](./exception-handling.md)：说明全局异常过滤器、异常消息映射、统一返回结构和全局异常处理器。
- [CORS 跨域访问](./cors.md)：说明跨域注册、配置项、凭据规则和 Token 响应头暴露。
- [自定义重写](./customization.md)：说明自定义统一返回 Provider、跳过统一返回和常见坑。
