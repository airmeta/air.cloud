# Web服务

`Air.Cloud.WebApp` 用于快速构建 Web API 服务。原来的长文档已经拆分到 `webapp` 子目录，便于按能力检索和维护。

## 阅读入口

- [Web服务总览](./webapp/README.md)：使用场景、能力拆分和旧入口替换规则。
- [创建 Web 服务](./webapp/getting-started.md)：新项目推荐写法、只启用核心能力和拆开注册。
- [统一返回配置](./webapp/unify-result.md)：默认返回结构、配置节、状态码适配和短路状态码边界。
- [DataValidation 数据验证](./webapp/data-validation.md)：模型验证、手动验证 API、验证失败结果和前端解析建议。
- [FriendlyException 友好异常](./webapp/friendly-exception.md)：`Oops` 使用方式、业务异常、普通友好异常和错误码。
- [异常处理](./webapp/exception-handling.md)：全局异常过滤器、异常消息映射、统一返回结构和全局异常处理器。
- [CORS 跨域访问](./webapp/cors.md)：跨域注册、配置项、凭据规则和响应头暴露。
- [自定义重写](./webapp/customization.md)：自定义统一返回 Provider、跳过统一返回和常见坑。

## 推荐顺序

1. 新项目先看 [创建 Web 服务](./webapp/getting-started.md)。
2. 需要统一响应结构时看 [统一返回配置](./webapp/unify-result.md)。
3. 表单、DTO、业务验证问题看 [DataValidation 数据验证](./webapp/data-validation.md)。
4. 业务异常和错误码问题看 [FriendlyException 友好异常](./webapp/friendly-exception.md)。
5. 全局异常响应和异常映射问题看 [异常处理](./webapp/exception-handling.md)。
6. 返回结构不符合项目规范时看 [自定义重写](./webapp/customization.md)。
