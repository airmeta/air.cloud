## Reflect.cs 


#### 描述:


内部反射静态类


#### 定义: 
``` csharp
public sealed class Reflect
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| GetEntryAssembly | 获取入口程序集 |
| GetAssembly | 根据程序集名称获取运行时程序集 |
| LoadAssembly | 根据路径加载程序集 |
| LoadAssembly | 通过流加载程序集 |
| GetAssemblyName | 获取程序集名称 |
| GetAssemblyName | 获取程序集名称 |
| GetAssemblyName | 获取程序集名称 |
| GetStringType | 加载程序集类型，支持格式：程序集;网站类型命名空间 |
---
### 方法详解 
####  GetEntryAssembly
* 方法描述:<br> 获取入口程序集
* 方法类型:静态方法
* 响应类型:<br> Assembly <br> (System.Reflection.Assembly)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  GetAssembly
* 方法描述:<br> 根据程序集名称获取运行时程序集
* 方法类型:静态方法
* 响应类型:<br> Assembly <br> (System.Reflection.Assembly)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| assemblyName | String |<br> |
####  LoadAssembly
* 方法描述:<br> 根据路径加载程序集
* 方法类型:静态方法
* 响应类型:<br> Assembly <br> (System.Reflection.Assembly)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| path | String |<br> |
####  LoadAssembly
* 方法描述:<br> 通过流加载程序集
* 方法类型:静态方法
* 响应类型:<br> Assembly <br> (System.Reflection.Assembly)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| assembly | MemoryStream |<br> |
####  GetAssemblyName
* 方法描述:<br> 获取程序集名称
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| assembly | Assembly |<br> |
####  GetAssemblyName
* 方法描述:<br> 获取程序集名称
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> |
####  GetAssemblyName
* 方法描述:<br> 获取程序集名称
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| typeInfo | TypeInfo |<br> |
####  GetStringType
* 方法描述:<br> 加载程序集类型，支持格式：程序集;网站类型命名空间
* 方法类型:静态方法
* 响应类型:<br> Type <br> (System.Type)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| str | String |<br> |
