# Docker 容器使用

## 低权限模式运行

?> 为何需要低权限模式运行?

因为Docker的用户组和宿主机保持一致,而Docker容器默认使用的是root账户进行运行,这存在使用root账户非法提权行为拿到整个宿主机的权限,并对系统进行破坏



### 配置低权限模式运行

为了方便管控,这里采用Docker Compose 来进行配置

1. 创建Docker组和Docker内的低权限用户
2. 授予用户目录访问权限
3. 查看组编号和低权限用户编号
4. 配置用户和组信息
5. 重新构建容器并运行
6. 检查 docker ps