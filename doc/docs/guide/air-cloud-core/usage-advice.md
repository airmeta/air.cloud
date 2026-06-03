# 使用建议

## 新项目

推荐先选择应用类型：

- Web API：从 `Air.Cloud.WebApp` 开始。
- Worker / 后台任务：从 `Air.Cloud.HostApp` 开始。

## 老项目迁移

建议分阶段：

1. 先引入 Core。
2. 再引入 WebApp 或 HostApp。
3. 再按需引入模块，例如 Redis、Kafka、Consul。
4. 最后再接入插件，例如 JWT、Swagger。

不要一次性引入所有模块。Air.Cloud 的优势是按需组合，不是全量堆叠。

::: danger 警告
Air.Cloud 更适合微服务或模块化服务场景。不要为了微服务而微服务，也不要为了“用了框架”而引入不需要的模块。
:::
