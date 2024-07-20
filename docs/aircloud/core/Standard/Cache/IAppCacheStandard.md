## IAppCacheStandard.cs 


#### 描述:


缓存标准


#### 定义: 
``` csharp
public  interface IAppCacheStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| SetCache | 设置缓存内容 |
| GetCache | 获取缓存内容 |
| SetCache |  |
| GetCache | 获取缓存内容 |
---
### 方法详解 
####  SetCache
* 方法描述:<br> 设置缓存内容
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> 缓存键|
| Content | String |<br> 缓存值|
| timeSpan | Nullable<TimeSpan> |<br> 有效期(不设置则为永久)|
####  GetCache
* 方法描述:<br> 获取缓存内容
* 方法类型:普通方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> 键|
####  SetCache
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| t | T |<br> |
| timeSpan | Nullable<TimeSpan> |<br> |
####  GetCache
* 方法描述:<br> 获取缓存内容
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> 键|
