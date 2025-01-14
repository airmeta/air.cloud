using Grpc.Core;
namespace unit.webapp.entry.Grpc
{
    //public class DynamicGrpcServer
    //{
    //    public Server Start(int port)
    //    {
    //        // 创建gRPC服务器实例
    //        var server = new Server
    //        {
    //            Services = { },
    //            Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
    //        };

    //        // 通过反射动态加载和注册服务
    //        var serviceType = typeof(YourServiceDefinition); // 你的gRPC服务接口类型
    //        var implementationType = typeof(YourServiceImplementation); // 你的gRPC服务实现类型
    //        var serviceDefinition = BuildServiceDefinition(serviceType, implementationType);

    //        server.Services.Add(serviceDefinition);

    //        // 启动服务器
    //        server.Start();
    //        return server;
    //    }

    //    private static ServerServiceDefinition BuildServiceDefinition(Type serviceType, Type implementationType)
    //    {
    //        // 使用反射创建服务实现的实例
    //        var implementation = Activator.CreateInstance(implementationType);

    //        // 使用反射调用 MethodConstructors.ForServiceMethod 方法获取方法构造器
    //        // 注意：MethodConstructors.ForServiceMethod 是gRPC的内部API，可能会在未来版本中发生变化
    //        var methodConstructors = GetMethodConstructors(serviceType);

    //        // 注册服务方法
    //        var serviceDefinition = ServerServiceDefinition.CreateBuilder()
    //            .AddMethod(methodConstructors.UnaryCallMethod, new UnaryServerCallHandler(
    //                (request, context) => ((UnaryServerMethod<object, object>)implementation).Invoke(request, context)))
    //            // 添加其他方法的处理逻辑
    //            .Build();

    //        return serviceDefinition;
    //    }

    //    // 假设的内部方法获取方法构造器，实际情况中需要根据gRPC的实际API来获取
    //    private static object GetMethodConstructors(Type serviceType)
    //    {
    //        // 反射调用gRPC内部方法获取MethodConstructors
    //        // 这里只是示例，实际情况需要根据gRPC的API来获取正确的方法
    //        // 请注意，这里的代码是假设的，并不代表实际可用的代码
    //        throw new NotImplementedException("请根据gRPC的实际API来实现方法构造器的获取");
    //    }
    //}

}
