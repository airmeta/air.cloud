using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Air.Cloud.Core.Standard.Event.Builders;

/// <summary>
/// 事件总线配置选项构建器
/// </summary>
public sealed class EventBusOptionsBuilder
{
    /// <summary>
    /// 事件处理器类型集合
    /// </summary>
    private readonly List<Type> _eventHandlers = new();

    /// <summary>
    /// 事件发布者类型
    /// </summary>
    private Type eventPublisher;

    /// <summary>
    /// 事件存储器实现工厂
    /// </summary>
    private Func<IServiceProvider, IEventSourceStorer> _eventSourceStorerImplementationFactory;

    /// <summary>
    /// 事件重试行为规则类型集合
    /// </summary>
    private readonly List<Type> _fallbackRuleTypes = new();

    /// <summary>
    /// 事件处理程序执行器
    /// </summary>
    private Type _eventHandlerExecutor;

    /// <summary>
    /// 默认内置事件源存储器内存通道容量
    /// </summary>
    /// <remarks>超过 n 条待处理消息，第 n+1 条将进入等待，默认为 3000</remarks>
    public int ChannelCapacity { get; set; } = 3000;

    /// <summary>
    /// 是否使用 UTC 时间戳，默认 false
    /// </summary>
    public bool UseUtcTimestamp { get; set; } = false;

    /// <summary>
    /// 是否启用模糊匹配消息
    /// </summary>
    /// <remarks>支持正则表达式</remarks>
    public bool FuzzyMatch { get; set; } = false;

    /// <summary>
    /// 是否启用执行完成触发 GC 回收
    /// </summary>
    public bool GCCollect { get; set; } = true;

    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    public bool LogEnabled { get; set; } = true;

    /// <summary>
    /// 重试失败行为规则配置
    /// </summary>
    public Type FallbackRule { get; set; }

    /// <summary>
    /// 未察觉任务异常事件处理程序
    /// </summary>
    public EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskExceptionHandler { get; set; }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <typeparam name="TEventSubscriber">实现自 <see cref="IEventHandler"/></typeparam>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddSubscriber<TEventSubscriber>()
        where TEventSubscriber : class, IEventHandler
    {
        _eventHandlers.Add(typeof(TEventSubscriber));
        return this;
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <param name="eventSubscriberType"><see cref="IEventHandler"/> 派生类型</param>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddSubscriber(Type eventSubscriberType)
    {
        // 类型检查
        if (!typeof(IEventHandler).IsAssignableFrom(eventSubscriberType) || eventSubscriberType.IsInterface)
        {
            throw new InvalidOperationException("The <eventSubscriberType> is not implement the IEventSubscriber interface.");
        }

        _eventHandlers.Add(eventSubscriberType);
        return this;
    }

    /// <summary>
    /// 批量注册事件处理器
    /// </summary>
    /// <param name="assemblies">程序集</param>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddHandlers(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new InvalidOperationException("The assemblies can be not null or empty.");
        }

        // 获取所有导出类型（非接口，非抽象类且实现 IEventSubscriber）接口
        var subscribers = assemblies.SelectMany(ass =>
              ass.GetExportedTypes()
                 .Where(t => t.IsPublic && t.IsClass && !t.IsInterface && !t.IsAbstract && typeof(IEventHandler).IsAssignableFrom(t)));

        foreach (var subscriber in subscribers)
        {
            _eventHandlers.Add(subscriber);
        }

        return this;
    }

    /// <summary>
    /// 批量注册事件处理器
    /// </summary>
    /// <param name="types">类型集合</param>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddHandlers(IEnumerable<Type> types)
    {
        if (types == null || types.Count() == 0)
        {
            throw new InvalidOperationException("The types can be not null or empty.");
        }

        // 获取所有导出类型（非接口，非抽象类且实现 IEventSubscriber）接口
        var subscribers = types
                 .Where(t => t.IsPublic && t.IsClass && !t.IsInterface && !t.IsAbstract && typeof(IEventHandler).IsAssignableFrom(t));

        foreach (var subscriber in subscribers)
        {
            _eventHandlers.Add(subscriber);
        }

        return this;
    }

    /// <summary>
    /// 替换事件发布者
    /// </summary>
    /// <typeparam name="TEventPublisher">实现自 <see cref="IEventPublisher"/></typeparam>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder ReplacePublisher<TEventPublisher>()
        where TEventPublisher : class, IEventPublisher
    {
        eventPublisher = typeof(TEventPublisher);
        return this;
    }

    /// <summary>
    /// 替换事件源存储器
    /// </summary>
    /// <param name="implementationFactory">自定义事件源存储器工厂</param>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder ReplaceStorer(Func<IServiceProvider, IEventSourceStorer> implementationFactory)
    {
        _eventSourceStorerImplementationFactory = implementationFactory;
        return this;
    }

    /// <summary>
    /// 替换事件源存储器（如果初始化失败则回退为默认的）
    /// </summary>
    /// <param name="createStorer"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public EventBusOptionsBuilder ReplaceStorerOrFallback(Func<IEventSourceStorer> createStorer)
    {
        // 空检查
        if (createStorer == null) throw new ArgumentNullException(nameof(createStorer));

        try
        {
            // 创建事件源存储器
            var storer = createStorer.Invoke();

            // 替换事件源存储器
            ReplaceStorer(_ => storer);
        }
        catch { }

        return this;
    }
    /// <summary>
    /// 注册事件处理程序执行器
    /// </summary>
    /// <typeparam name="TEventHandlerExecutor">实现自 <see cref="IEventHandlerExecutor"/></typeparam>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddExecutor<TEventHandlerExecutor>()
        where TEventHandlerExecutor : class, IEventHandlerExecutor
    {
        _eventHandlerExecutor = typeof(TEventHandlerExecutor);
        return this;
    }

    /// <summary>
    /// 注册事件重试行为规则
    /// </summary>
    /// <typeparam name="TEventFallbackRule">实现自 <see cref="IEventFallbackRule"/></typeparam>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddFallbackRule<TEventFallbackRule>()
        where TEventFallbackRule : class, IEventFallbackRule
    {
        _fallbackRuleTypes.Add(typeof(TEventFallbackRule));
        return this;
    }

    /// <summary>
    /// 注册事件重试行为规则
    /// </summary>
    /// <param name="fallbackRuleType"><see cref="IEventFallbackRule"/> 派生类型</param>
    /// <returns><see cref="EventBusOptionsBuilder"/> 实例</returns>
    public EventBusOptionsBuilder AddFallbackRule(Type fallbackRuleType)
    {
        // 类型检查
        if (!typeof(IEventFallbackRule).IsAssignableFrom(fallbackRuleType) || fallbackRuleType.IsInterface)
        {
            throw new InvalidOperationException("The <fallbackRuleType> is not implement the IEventFallbackRule interface.");
        }

        _fallbackRuleTypes.Add(fallbackRuleType);
        return this;
    }

    /// <summary>
    /// 构建事件总线配置选项
    /// </summary>
    /// <param name="services">服务集合对象</param>
    public  void Build(IServiceCollection services)
    {
        // 注册事件处理器
        foreach (var eventHandler in _eventHandlers)
        {
            services.AddSingleton(typeof(IEventHandler), eventHandler);
        }

        // 替换事件发布者
        if (eventPublisher != default)
        {
            services.Replace(ServiceDescriptor.Singleton(typeof(IEventPublisher), eventPublisher));
        }

        // 替换事件存储器
        if (_eventSourceStorerImplementationFactory != default)
        {
            services.Replace(ServiceDescriptor.Singleton(_eventSourceStorerImplementationFactory));
        }
        // 注册事件执行器
        if (_eventHandlerExecutor != default)
        {
            services.AddSingleton(typeof(IEventHandlerExecutor), _eventHandlerExecutor);
        }

        // 注册事件重试行为规则
        foreach (var fallbackRuleType in _fallbackRuleTypes)
        {
            services.AddSingleton(fallbackRuleType);
        }
    }
}