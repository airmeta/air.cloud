
### 使用流程

#### 类库创建 
    分别创建以下类库    
    1. 服务启动层
    2. 接口服务层
    3. 业务领域层
    4. 实体模型层
    5. 数据仓储层
    6. 公共类库

#### 引用关系

    根据 类库创建 的数字顺序进行引用,1引用2,2引用3,3引用4,4引用5,5引用6

::: warning 注意
 类库引用顺序自上而下,而不能自下而上,否则将会产生类库循环引用,多个服务之间业务接口可能会产生职责污染
::: 

#### 公共类库配置

以下xml只提供最基础的部分,一般都放置在公共类库中,这样可以减少引用
``` xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Air.Cloud.Core" Version="1.0.2" />
    <PackageReference Include="Air.Cloud.DataBase"  Version="1.0.2" />
    <PackageReference Include="Air.Cloud.Core.Extensions"  Version="1.0.2" />
    <PackageReference Include="Air.Cloud.Modules.Kafka"  Version="1.0.2" />
    <PackageReference Include="Air.Cloud.Modules.RedisCache"  Version="1.0.2" />
    <PackageReference Include="Air.Cloud.Plugins.Jwt"  Version="1.0.2" />
    <PackageReference Include="Air.Cloud.Plugins.SpecificationDocument" Version="1.0.2" />
  </ItemGroup>

</Project>

```
#### 安装服务解析

在服务启动层安装 Air.Cloud.WebApp 或者 Air.Cloud.HostApp
::: warning 注意

 目前使用的最多的是WebApp,HostApp则需要进行测试

::: 

#### 配置Startup.cs

该配置项虽说在后来的.NET 版本中已经被删除,但是Air.Cloud中还是明确会保留该项,因为这可以将整个框架的所有成员以固定顺序进行编排,避免了一些潜在性的错误

不过Startup.cs不是必须的,仅仅在你需要注入的时候使用,通过实现AppStartup抽象类去实现注入方法,通过AppStartupAttribute 对成员进行以任意顺序进行编排

::: tip 提示
AppStartupAttribute的Order越大越先执行
:::

``` csharp

    [AppStartup(Order =int.MinValue)]
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }

```


####

#### 配置Program.cs 


``` csharp
using Air.Cloud.WebApp.App;

var builder = WebApplication.CreateBuilder(args);

var app = builder.WebInjectInFile();
app.Run();
```



#### 开始启动项目

::: tip 提示
该文档为大概的启动顺序,需要你参考Github中的框架代码,并进行适当的调整,有问题请提交ISSUES
::: 