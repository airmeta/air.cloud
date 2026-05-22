### 核心构成

 Air.Cloud通过核心库定义标准,各模组实现标准的方式,可以服务按需裁剪发布运行,大大减小了发布包的体积

### 背景:
        
        在互联网应用大规模化需求逐渐普及的今天,传统的三层架构已经无法满足当下的系统要求.
        流量、速度、安全等一些问题日渐凸显,我们希望构建一个可以由使用者自由搭配的框架
        
### 概念:

    标准: 实现某个功能的抽象类/接口类
    模组: 实现某个标准的类库
    插件: 轻量化的模组,一般用于Web服务中,非Web服务运行的必要实现. 
          例如:JWT身份认证 没有这个功能不影响你的实际业务运行.

### 基础信息

Github仓库: [Air.Cloud](https://github.com/AccessCross/air.cloud)

nuget仓库: [Air.Cloud](https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true)

许可证信息: [MPL2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE)

### 环境要求

开发工具: VisualStudio 2022、VisualStudio Insiders

环境: .NET 6.0

### 程序类型

Air.Cloud支持以下三种类型的应用程序,你可以根据你的需要去选择它

1. Web服务应用程序:
        
        使用Air.Cloud.WebApp模块可以创建并运行Web服务应用程序

2. 控制台应用程序:
        
        使用Air.Cloud.HostApp模块可以创建并运行控制台应用程序

3. GRPC应用程序
        
        仅需Air.Cloud.Core即可运行GRPC应用程序

---

### 加载机制
通过实现IAppInjectStandard标准来控制系统加载行为,该标准分别由Air.Cloud.HostApp和Air.Cloud.WebApp这两个直接类库进行实现,如果是GRPC则无需关注此标准
### 框架组件
---

<div style="display:flex;justify-content:center;">

<img src="/assets/modules/whiteboard_exported_image.png" style="width:1261px;height:749px;"/>
</div>


::: danger 警告
该框架适用于微服务架构场景中应用,不太适用于在单体架构中应用.但是无论哪种系统架构均有优缺点,我们对于框架使用需要结合实际场景慎重考虑

    不要为了微服务而微服务

:::
