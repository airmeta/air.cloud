## AppObjectExtensions.cs 


#### 描述:


对象拓展类


#### 定义: 
``` csharp
public sealed class AppObjectExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| IsRichPrimitive | 判断是否是富基元类型 |
| Connect |  |
| Concat |  |
| IsAsync | 判断方法是否是异步 |
| HasImplementedRawGeneric | 判断类型是否实现某个泛型 |
| GetAncestorTypes | 获取所有祖先类型 |
| GetRealReturnType | 获取方法真实返回类型 |
| ChangeType |  |
| ChangeType | 将一个对象转换为指定类型 |
| GetFoundAttribute |  |
| ToObject | JsonElement 转 Object |
---
### 方法详解 
####  IsRichPrimitive
* 方法描述:<br> 判断是否是富基元类型
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> 类型|
####  Connect
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Dictionary`2<String>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dic | Dictionary`2<String> |<br> |
| newDic | IDictionary`2<String> |<br> |
####  Concat
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dic | ConcurrentDictionary`2<String> |<br> |
| newDic | Dictionary`2<String> |<br> |
####  IsAsync
* 方法描述:<br> 判断方法是否是异步
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> 方法|
####  HasImplementedRawGeneric
* 方法描述:<br> 判断类型是否实现某个泛型
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> 类型|
| generic | Type |<br> 泛型类型|
####  GetAncestorTypes
* 方法描述:<br> 获取所有祖先类型
* 方法类型:静态方法
* 响应类型: IEnumerable<Type>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> |
####  GetRealReturnType
* 方法描述:<br> 获取方法真实返回类型
* 方法类型:静态方法
* 响应类型:<br> Type <br> (System.Type)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> |
####  ChangeType
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| obj | Object |<br> |
####  ChangeType
* 方法描述:<br> 将一个对象转换为指定类型
* 方法类型:静态方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| obj | Object |<br> 待转换的对象|
| type | Type |<br> 目标类型|
####  GetFoundAttribute
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TAttribute <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> |
| inherit | Boolean |<br> |
####  ToObject
* 方法描述:<br> JsonElement 转 Object
* 方法类型:静态方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| jsonElement | JsonElement |<br> |
