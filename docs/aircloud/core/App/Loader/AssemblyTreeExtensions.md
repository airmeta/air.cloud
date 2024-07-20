## AssemblyTreeExtensions.cs 


#### 描述:


zh-cn: 程序集树扩展
en-us: Assembly tree extension


#### 定义: 
``` csharp
public sealed class AssemblyTreeExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Order | zh-cn: 程序集树排序<br>en-us: Assembly tree sorting |
| LoadTree | zh-cn:加载程序集依赖树信息<br>en-us:Load assembly dependency tree information |
| OrderTree | zh-cn: 调整树的顺序<br>en-us: Adjust the order of the tree |
---
### 方法详解 
####  Order
* 方法描述:<br> zh-cn: 程序集树排序<br>en-us: Assembly tree sorting
* 方法类型:静态方法
* 响应类型:<br> AssemblyTree <br> (Air.Cloud.Core.App.Loader.AssemblyTree)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| tree | AssemblyTree |<br> zh-cn: 程序集树<br>en-us: Assembly tree|
| NodeDependency | IDictionary`2<String> |<br> zh-cn:程序集依赖程序集信息<br>en-us:assembly dependent assembly information|
| Used | List<AssemblyName> |<br> zh-cn: 当前程序集是否已经被其他程序集引用,如果引用了则不再向下查找<br>en-us: Whether the current assembly has been referenced by other assemblies, if it has been referenced, it will not be searched down|
####  LoadTree
* 方法描述:<br> zh-cn:加载程序集依赖树信息<br>en-us:Load assembly dependency tree information
* 方法类型:静态方法
* 响应类型:<br> AssemblyTree <br> (Air.Cloud.Core.App.Loader.AssemblyTree)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| assembly | AssemblyName |<br> zh-cn:父级程序集信息<br>en-us:Parent assembly information|
| processedAssemblies | HashSet<String> |<br> zh-cn:已处理的程序集<br>en-us:Processed assembly|
####  OrderTree
* 方法描述:<br> zh-cn: 调整树的顺序<br>en-us: Adjust the order of the tree
* 方法类型:静态方法
* 响应类型:<br> AssemblyTree <br> (Air.Cloud.Core.App.Loader.AssemblyTree)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| tree | AssemblyTree |<br> zh-cn:待调整<br>en-us:To be adjusted|
