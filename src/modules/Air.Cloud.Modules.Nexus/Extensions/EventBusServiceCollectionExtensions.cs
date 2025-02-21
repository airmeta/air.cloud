using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Core.Standard.Event.Builders;
using Air.Cloud.Modules.Nexus.Handlers;
using Air.Cloud.Modules.Nexus.Publishers;
using Air.Cloud.Modules.Nexus.Publishers.Storers;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Nexus.Extensions;

/// <summary>
/// EventBus 应用行为服务拓展
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 添加 EventBus 应用行为注册
    /// </summary>
    /// <param name="services">服务集合对象</param>
    /// <param name="configureOptionsBuilder">事件总线配置选项构建器委托</param>
    /// <returns>服务集合实例</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptionsBuilder> configureOptionsBuilder)
    {
        // 创建初始事件总线配置选项构建器
        var eventBusOptionsBuilder = new EventBusOptionsBuilder();
        configureOptionsBuilder.Invoke(eventBusOptionsBuilder);

        return services.AddEventBus(eventBusOptionsBuilder);
    }

    /// <summary>
    /// 添加 EventBus 应用行为注册
    /// </summary>
    /// <param name="services">服务集合对象</param>
    /// <param name="eventBusOptionsBuilder">事件总线配置选项构建器</param>
    /// <returns>服务集合实例</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services, EventBusOptionsBuilder eventBusOptionsBuilder = default)
    {
        // 初始化事件总线配置项
        eventBusOptionsBuilder ??= new EventBusOptionsBuilder();

        // 注册内部服务
        services.AddInternalService(eventBusOptionsBuilder);

        // 构建事件总线服务
        eventBusOptionsBuilder.Build(services);

        services.AddSingleton<IEventBusTaskBackGroundService>(serviceProvider =>
        {
            // 创建事件总线后台服务对象
            var eventBusHostedService = ActivatorUtilities.CreateInstance<EventBusTaskBackGroundService>(
                serviceProvider
                , eventBusOptionsBuilder.UseUtcTimestamp
                , eventBusOptionsBuilder.FuzzyMatch
                , eventBusOptionsBuilder.GCCollect
                , eventBusOptionsBuilder.LogEnabled);

            // 订阅未察觉任务异常事件
            var unobservedTaskExceptionHandler = eventBusOptionsBuilder.UnobservedTaskExceptionHandler;
            if (unobservedTaskExceptionHandler != default)
            {
                eventBusHostedService.UnobservedTaskException += unobservedTaskExceptionHandler;
            }

            return eventBusHostedService;
        });
        return services;
    }

    /// <summary>
    /// 注册内部服务
    /// </summary>
    /// <param name="services">服务集合对象</param>
    /// <param name="eventBusOptionsBuilder">事件总线配置选项构建器</param>
    /// <returns>服务集合实例</returns>
    private static IServiceCollection AddInternalService(this IServiceCollection services, EventBusOptionsBuilder eventBusOptionsBuilder)
    {
        // 创建默认内存通道事件源对象
        var defaultStorerOfChannel = new ChannelEventSourceStorer(eventBusOptionsBuilder.ChannelCapacity);

        // 注册后台任务队列接口/实例为单例，采用工厂方式创建
        services.AddSingleton<IEventSourceStorer>(_ =>
        {
            return defaultStorerOfChannel;
        });

        // 注册默认内存通道事件发布者
        services.AddSingleton<IEventPublisher, DefaultEventPublisher>();

        return services;
    }
}

