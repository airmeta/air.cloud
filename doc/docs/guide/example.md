### 代码结构

#### 解决方案约定
单个服务在解决方案里面体现为5+1的模式

由于在大量服务存在的情况下,普通项目的3层结构已经无法支持该行为,需要细化到5层,分别为

    1. 服务启动层
    2. 接口服务层
    3. 业务领域层
    4. 实体模型层
    5. 数据仓储层

另外,由于各个服务之间存在公共代码,例如工具类等,所以需要一个公共库来进行解耦:

    1. 公共库

::: warning 注意
公共库只能被引用,不可以主动引用任何接口服务层,因为这将会导致引用的接口在多个服务中一并出现
:::

关于服务的5层,可以根据情况适当的减少你的层数,比如服务接口较少或者不与任何其他业务存在连接的情况下,可以将所有的代码除了公共库之外都编写在服务启动层

    例如: 文件上传服务


#### 命名规范
    
    客户名.系统名.服务名.作用标识

作用标识: 
| Class      | Description |
| ----------- | ----------- |
| Entry | 启动类库(Web API) |
| Service | 服务接口(类库) |
| Model |  实体模型(类库)|
| Domain | 业务领域(类库) |
| Repository | 数据仓储(类库) |

假如有个客户公司 [SANSHI] 需要做一个项目 [WUYE] 

系统管理服务:<br> 
    
    SANSHI.WUYE.System.Entry-----启动WebApi
    SANSHI.WUYE.System.Service-----接口服务
    SANSHI.WUYE.System.Model-----实体模型
    SANSHI.WUYE.System.Domain-----业务领域
    SANSHI.WUYE.System.Repository-----数据仓储

工作流服务:<br> 
    
    SANSHI.WUYE.Workflow.Entry-----启动WebApi
    SANSHI.WUYE.Workflow.Service-----接口服务
    SANSHI.WUYE.Workflow.Model-----实体模型
    SANSHI.WUYE.Workflow.Domain-----业务领域
    SANSHI.WUYE.Workflow.Repository-----数据仓储

::: tip 提示
对于多服务的场景下,你可能需要一个common类库来解耦各个服务之间的共性,
我们可以定义一个公共类库

    例如: SANSHI.WUYE.Common
:::

