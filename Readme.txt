
1. 当前解决方案文件夹下的两个类库为容器读取类库. 只需要在你的api程序中引用这两个类库即可完成调用

由于这两个类库分别是基于Windows 和 Linux平台编写的, 所以在使用时需要根据你的运行环境选择合适的类库引用.

在你的Startup.cs文件中,你需要Add相关类库的服务,然后你就可以在你的控制器或者其他地方使用这两个类库了.


Docker 类库使用代码:
    DockerContainer<DockerContainerInstance> container = new DockerContainer<DockerContainerInstance>();
    await container.Load();
   
IIS 类库使用代码:
	IISContainer<IISContainerInstance> container = new IISContainer<IISContainerInstance>();
	await container.Load();

详细参考unit里面的测试类库调用方案

后续更新计划是将会统一这两个类库的调用方式,并且简化一部分代码. 但是目前的版本已经可以满足大部分的需求了.