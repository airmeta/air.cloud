## IRedisCacheStandard.cs 


#### 描述:


Redis缓存标准


#### 定义: 
``` csharp
public  interface IRedisCacheStandard
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Hash |  Air.Cloud.Core.Standard.Cache.Redis.IHashCache | zh-cn: Hash 存储<br>en-us: Hash stored |
|     String |  Air.Cloud.Core.Standard.Cache.Redis.IStringCache | zh-cn: String 存储<br>en-us: String stored |
|     List |  Air.Cloud.Core.Standard.Cache.Redis.IListCache | zh-cn: List 存储<br>en-us: List stored |
|     Set |  Air.Cloud.Core.Standard.Cache.Redis.ISetCache | zh-cn: Set 存储<br>en-us: Set stored |
|     SortedSet |  Air.Cloud.Core.Standard.Cache.Redis.ISortedSetCache | zh-cn: SortedSet 存储<br>en-us: SortedSet stored |
|     Key |  Air.Cloud.Core.Standard.Cache.Redis.IRedisCacheKeyStandard |  |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Change | 更换数据库 |
---
### 方法详解 
####  Change
* 方法描述:<br> 更换数据库
* 方法类型:普通方法
* 响应类型:<br> IRedisCacheStandard <br> (Air.Cloud.Core.Standard.Cache.Redis.IRedisCacheStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| DataBaseIndex | Int32 |<br> |
