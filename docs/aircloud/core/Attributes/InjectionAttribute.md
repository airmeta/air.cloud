## InjectionAttribute.cs 


#### 描述:


设置依赖注入方式


#### 定义: 
``` csharp
public  class InjectionAttribute
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Action |  Air.Cloud.Core.Dependencies.Enums.InjectionActions | 添加服务方式，存在不添加，或继续添加 |
|     Pattern |  Air.Cloud.Core.Dependencies.Enums.InjectionPatterns | 注册选项 |
|     Named |  System.String | 注册别名 |
|     Order |  System.Int32 | 排序，排序越大，则在后面注册 |
|     ExceptInterfaces |  System.Type[] | 排除接口 |
|     Proxy |  System.Type | 代理类型，必须继承 DispatchProxy、IDispatchProxy |
|     TypeId |  System.Object |  |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Match |  |
| IsDefaultAttribute |  |
---
### 方法详解 
####  Match
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| obj | Object |<br> |
####  IsDefaultAttribute
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
