## MapPlugin.cs 


#### 描述:


地图插件


#### 定义: 
``` csharp
public  class MapPlugin
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| GetDistance | 计算两个坐标点之间的距离 |
| GetPointDistance | 计算两个坐标点之间的距离 |
| ComputeTextSame | 计算文本相似度函数(适用于短文本) |
| MarsToBaidu |  |
| BaiduToMars |  |
| BaiduToMars |  |
---
### 方法详解 
####  GetDistance
* 方法描述:<br> 计算两个坐标点之间的距离
* 方法类型:静态方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| firstLatitude | Double |<br> 第一个坐标的纬度|
| firstLongitude | Double |<br> 第一个坐标的经度|
| secondLatitude | Double |<br> 第二个坐标的纬度|
| secondLongitude | Double |<br> 第二个坐标的经度|
####  GetPointDistance
* 方法描述:<br> 计算两个坐标点之间的距离
* 方法类型:静态方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| firstPoint | String |<br> 第一个坐标点的（纬度,经度）|
| secondPoint | String |<br> 第二个坐标点的（纬度,经度）|
####  ComputeTextSame
* 方法描述:<br> 计算文本相似度函数(适用于短文本)
* 方法类型:静态方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| textX | String |<br> 文本|
| textY | String |<br> 文本|
| isCase | Boolean |<br> 是否忽略大小写|
####  MarsToBaidu
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> CoordinateDto <br> (Air.Cloud.Core.Plugins.Map.CoordinateDto)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| marsCoordinate | CoordinateDto |<br> |
####  BaiduToMars
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> CoordinateDto <br> (Air.Cloud.Core.Plugins.Map.CoordinateDto)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| baiduCoordinate | CoordinateDto |<br> |
####  BaiduToMars
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> CoordinateDto <br> (Air.Cloud.Core.Plugins.Map.CoordinateDto)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| longitude | Double |<br> |
| latitude | Double |<br> |
