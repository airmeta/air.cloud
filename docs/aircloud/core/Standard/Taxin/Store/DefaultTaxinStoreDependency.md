## DefaultTaxinStoreDependency.cs 


#### 描述:


zh-cn:默认Taxin 存储实现
en-us:Default Taxin store dependency


#### 定义: 
``` csharp
public  class DefaultTaxinStoreDependency
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Options |  Air.Cloud.Modules.Taxin.TaxinOptions | zh-cn:Taxin 配置选项<br>en-us:Taxin options  |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| add_SetPersistenceHandler |  |
| remove_SetPersistenceHandler |  |
| add_GetPersistenceHandler |  |
| remove_GetPersistenceHandler |  |
| DefaultSetPersistenceHandler | zh-cn:默认Taxin存储Set事件<br>en-us:By default, Taxin stores the Set event |
| DefaultGetPersistenceHandler | zh-cn:默认Taxin存储Get事件<br>en-us:By default, Taxin stores the Get event |
| GetPersistenceAsync |  |
| SetPersistenceAsync |  |
---
### 方法详解 
####  add_SetPersistenceHandler
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | EventHandler<TaxinStoreEventArgs> |<br> |
####  remove_SetPersistenceHandler
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | EventHandler<TaxinStoreEventArgs> |<br> |
####  add_GetPersistenceHandler
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | EventHandler<TaxinStoreEventArgs> |<br> |
####  remove_GetPersistenceHandler
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | EventHandler<TaxinStoreEventArgs> |<br> |
####  DefaultSetPersistenceHandler
* 方法描述:<br> zh-cn:默认Taxin存储Set事件<br>en-us:By default, Taxin stores the Set event
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| sender | Object |<br> zh-cn:事件触发人<br>en-us:Event sender|
| classInfoEvent | TaxinStoreEventArgs |<br> zh-cn:事件参数<br>en-us:Event args|
####  DefaultGetPersistenceHandler
* 方法描述:<br> zh-cn:默认Taxin存储Get事件<br>en-us:By default, Taxin stores the Get event
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| sender | Object |<br> zh-cn:事件触发人<br>en-us:Event sender|
| classInfoEvent | TaxinStoreEventArgs |<br> zh-cn:事件参数<br>en-us:Event args|
####  GetPersistenceAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<IDictionary`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  SetPersistenceAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Packages | IDictionary`2<String> |<br> |
