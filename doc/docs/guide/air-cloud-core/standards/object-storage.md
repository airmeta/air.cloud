# 对象存储标准

对象存储标准用于抽象文件、对象、Bucket 和存储客户端能力，避免业务直接绑定某个对象存储 SDK。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IAmazonS3Standard` | S3 综合能力入口 |
| `IAmazonS3ClientStandard` | S3 客户端标准 |
| `IAmazonS3BucketStandard` | Bucket 管理标准 |
| `IAmazonS3ObjectStandard` | Object 操作标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.AmazonS3` | Amazon S3 对象存储实现 |

## 使用建议

- 业务层应抽象文件用途，不应直接拼接 Bucket、Object Key 和存储地址。
- 上传、下载、删除等操作要明确异常补偿策略。
