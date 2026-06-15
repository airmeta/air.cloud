### Alibaba Nacos

Nacos 是阿里巴巴开源的服务发现、配置管理和服务治理平台，常用于微服务系统中的注册中心和配置中心。

在 Air.Cloud 中，`Air.Cloud.Modules.Nacos` 把 Nacos Naming 适配为 `IServerCenterStandard`，把 Nacos Config 适配为 `IKVCenterStandard`。模块适合已经使用 Nacos 作为注册中心或配置中心的系统，也适合需要从 Consul 迁移到 Nacos 的服务治理场景。

[官方文档链接](https://nacos.io/docs/latest/what-is-nacos/)
