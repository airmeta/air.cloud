## ITaxinStoreStandard.cs 


#### 描述:


zh-cn:Taxin 存储标准
en-us:Taxin store


#### 定义: 
``` csharp
public  class ITaxinStoreStandard
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Packages |  System.Collections.Generic.IDictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Collections.Generic.IEnumerable | zh-cn:数据包<br>en-us:Taxin data packages |
|     Current |  Air.Cloud.Modules.Taxin.Model.TaxinRouteDataPackage | zh-cn:当前数据包<br>en-us:Taxin data packages |
|     Routes |  System.Collections.Generic.IDictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Collections.Generic.IDictionary`2[[System.Version, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Air.Cloud.Modules.Taxin.Model.TaxinRouteInformation, Air.Cloud.Core, Version=1.0.0.2, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]] | zh-cn:路由表信息<br>en-us:route data |
|     CheckTag |  System.String | zh-cn:检查标记<br>en-us:CheckTag |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| SetPersistenceAsync | zh-cn:持久化数据存储<br>en-us: Persistent data storage  |
| GetPersistenceAsync | zh-cn:持久化数据存储<br>en-us: Persistent data storage  |
| add_SetPersistenceHandler |  |
| remove_SetPersistenceHandler |  |
| add_GetPersistenceHandler |  |
| remove_GetPersistenceHandler |  |
---
### 方法详解 
####  SetPersistenceAsync
* 方法描述:<br> zh-cn:持久化数据存储<br>en-us: Persistent data storage 
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Packages | IDictionary`2<String> |<br> zh-cn: Taxin 数据包<br>en-us: Taxin data packages|
####  GetPersistenceAsync
* 方法描述:<br> zh-cn:持久化数据存储<br>en-us: Persistent data storage 
* 方法类型:异步方法
* 响应类型: Task<IDictionary`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
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
