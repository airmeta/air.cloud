/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Core.Standard.Event.Attributes;
using Air.Cloud.Core.Standard.Event.Builders;
using Air.Cloud.Core.Standard.Event.Contexts;
using Air.Cloud.Modules.Nexus.Wrappers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Air.Cloud.Modules.Nexus.Handlers;

/// <summary>
/// 事件总线后台主机服务
/// </summary>
internal sealed class EventBusTaskBackGroundService : IEventBusExecutor
{
    /// <summary>
    /// GC 回收默认间隔
    /// </summary>
    private const int GC_COLLECT_INTERVAL_SECONDS = 3;

    /// <summary>
    /// 避免由 CLR 的终结器捕获该异常从而终止应用程序，让所有未觉察异常被觉察
    /// </summary>
    internal event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorager _eventSourceStorer;

    /// <summary>
    /// 事件发布服务
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 事件处理程序集合
    /// </summary>
    private readonly ConcurrentDictionary<EventHandlerWrapper, EventHandlerWrapper> _eventHandlers = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="eventSourceStorer">事件源存储器</param>
    /// <param name="eventPublisher">事件发布服务</param>
    /// <param name="eventHandlers">事件处理器集合</param>
    /// <param name="useUtcTimestamp">是否使用 Utc 时间</param>
    /// <param name="fuzzyMatch">是否启用模糊匹配事件消息</param>
    /// <param name="gcCollect">是否启用执行完成触发 GC 回收</param>
    /// <param name="logEnabled">是否启用日志记录</param>
    public EventBusTaskBackGroundService(IServiceProvider serviceProvider
        , IEventSourceStorager eventSourceStorer
        , IEventPublisher eventPublisher
        , IEnumerable<IMessageSubscriber> eventHandlers
        , bool useUtcTimestamp
        , bool fuzzyMatch
        , bool gcCollect
        , bool logEnabled)
    {
        _serviceProvider = serviceProvider;
        _eventPublisher = eventPublisher;
        _eventSourceStorer = eventSourceStorer;

        Executor = serviceProvider.GetService<IMessageExecutor>();
        UseUtcTimestamp = useUtcTimestamp;
        FuzzyMatch = fuzzyMatch;
        GCCollect = gcCollect;
        LogEnabled = logEnabled;

        BuildEventHandlers(eventHandlers, serviceProvider);
    }
    /// <summary>
    /// 事件处理程序执行器
    /// </summary>
    private IMessageExecutor Executor { get; }

    /// <summary>
    /// 是否使用 UTC 时间
    /// </summary>
    private bool UseUtcTimestamp { get; }

    /// <summary>
    /// 是否启用模糊匹配事件消息
    /// </summary>
    private bool FuzzyMatch { get; }

    /// <summary>
    /// 是否启用执行完成触发 GC 回收
    /// </summary>
    private bool GCCollect { get; }

    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    private bool LogEnabled { get; }

    /// <summary>
    /// 最近一次收集时间
    /// </summary>
    private DateTime? LastGCCollectTime { get; set; }

    /// <summary>
    /// 执行后台任务
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppRealization.Output.Print("EventBus Output", "EventBus task service is running.", Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information);

        // 注册后台主机服务停止监听
        stoppingToken.Register(() =>
            AppRealization.Output.Print("EventBus Output", $"EventBus task service is stopping.", Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information)
        );

        // 监听服务是否取消
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 执行具体任务
                await BackgroundProcessing(stoppingToken);
            }
            catch
            {
                Thread.Sleep(2000);
            }
        }
        AppRealization.Output.Print("EventBus Output", $"EventBus hosted service is stopped.");
    }

    /// <summary>
    /// 后台调用处理程序
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        // 从事件存储器中读取一条
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        var eventSource = await _eventSourceStorer.ReadAsync(stoppingToken);

        // 空检查
        if (string.IsNullOrWhiteSpace(eventSource?.EventName))
        {
            AppRealization.Output.Print("EventBus Output", "Invalid EventName, EventName cannot be <null> or an empty string.", Core.Standard.Print.AppPrintInformation.AppPrintLevel.Warning);

            return;
        }

        // 查找事件 Id 匹配的事件处理程序
        var eventHandlersThatShouldRun = _eventHandlers.Where(t => t.Key.ShouldRun(eventSource.EventName)).OrderByDescending(u => u.Value.Order)
            .Select(u => u.Key)
            .ToList();

        // 空订阅
        if (!eventHandlersThatShouldRun.Any() && !eventSource.IgnoreNotFound)
        {
            AppRealization.Output.Print("EventBus Output", "Subscriber with event Name <{EventName}> was not found.", Core.Standard.Print.AppPrintInformation.AppPrintLevel.Warning, AdditionalParams: new Dictionary<string, object>()
            {
                { "event-name",eventSource.EventName}
            });

            return;
        }

        // 检查是否配置只消费一次
        if (eventSource.IsConsumOnce)
        {
            var randomId = RandomNumberGenerator.GetInt32(0, eventHandlersThatShouldRun.Count);
            eventHandlersThatShouldRun = EventHandlerWrapper.CreateList(eventHandlersThatShouldRun.ElementAt(randomId));
        }

        // 创建一个任务工厂并保证执行任务都使用当前的计划程序
        var taskFactory = new TaskFactory(TaskScheduler.Current);

        // 创建共享上下文数据对象
        var properties = new Dictionary<object, object>();

        // 通过并行方式提高吞吐量并解决 Thread.Sleep 问题
        Parallel.ForEach(eventHandlersThatShouldRun, (eventHandlerThatShouldRun) =>
        {
            // 创建新的线程执行
            taskFactory.StartNew(async () =>
            {
                // 获取特性信息，可能为 null
                var eventSubscribeAttribute = eventHandlerThatShouldRun.Attribute;

                // 创建执行前上下文
                var eventHandlerExecutingContext = new EventHandlerExecutingContext(eventSource, properties, eventHandlerThatShouldRun.HandlerMethod, eventSubscribeAttribute)
                {
                    ExecutingTime = UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now
                };

                // 执行异常对象
                InvalidOperationException executionException = default;

                try
                {
                    // 处理任务取消
                    eventSource.CancellationToken.ThrowIfCancellationRequested();

                    await eventHandlerThatShouldRun.Handler!(eventHandlerExecutingContext);
                    // 触发事件处理程序事件
                    _eventPublisher.InvokeEvents(new(eventSource, true));
                }
                catch (Exception ex)
                {
                    // 输出异常日志
                    AppRealization.Output.Print("EventBus Output", "Error occurred executing in {EventName}.", Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error, AdditionalParams: new Dictionary<string, object>()
                    {
                        { "event-name",eventSource.EventName},
                        { "error",ex }
                    });

                    // 标记异常
                    executionException = new InvalidOperationException(string.Format("Error occurred executing in {0}.", eventSource.EventName), ex);

                    // 捕获 Task 任务异常信息并统计所有异常
                    if (UnobservedTaskException != default)
                    {
                        var args = new UnobservedTaskExceptionEventArgs(
                            ex as AggregateException ?? new AggregateException(ex));

                        UnobservedTaskException.Invoke(this, args);
                    }

                    // 触发事件处理程序事件
                    _eventPublisher.InvokeEvents(new(eventSource, false)
                    {
                        Exception = ex
                    });
                }
                finally
                {
                    // 判断是否执行完成后调用 GC 回收
                    var nowTime = DateTime.UtcNow;
                    if (eventHandlerThatShouldRun.GCCollect && (LastGCCollectTime == null || (nowTime - LastGCCollectTime.Value).TotalSeconds > GC_COLLECT_INTERVAL_SECONDS))
                    {
                        LastGCCollectTime = nowTime;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }, stoppingToken);
        });
    }
    /// <summary>
    /// 构建所有的事件处理器
    /// </summary>
    /// <param name="messageSubscribers"></param>
    private void BuildEventHandlers(IEnumerable<IMessageSubscriber> messageSubscribers, IServiceProvider serviceProvider)
    {
        var bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        // 逐条获取事件处理程序并进行包装
        foreach (var eventHandler in messageSubscribers)
        {
            // 获取事件处理器类型
            var eventSubscriberType = eventHandler.GetType();

            // 查找所有公开且贴有 [EventSubscribe] 的实例方法
            var eventHandlerMethods = eventSubscriberType.GetMethods(bindingAttr)
                .Where(u => u.IsDefined(typeof(EventDescriptionAttribute), false));

            // 遍历所有事件处理器处理方法
            foreach (var eventHandlerMethod in eventHandlerMethods)
            {
                // 将方法转换成 Func<EventHandlerExecutingContext, Task> 委托
                var internalhandler = (Func<EventHandlerExecutingContext, Task>)eventHandlerMethod.CreateDelegate(typeof(Func<EventHandlerExecutingContext, Task>), eventHandler);
                // 处理同一个事件处理程序支持多个事件 Id 情况
                var eventSubscribeAttributes = eventHandlerMethod.GetCustomAttributes<EventDescriptionAttribute>(false);
                // 获取时间处理中间件
                var eventMiddlewaresAttributes = eventHandlerMethod.GetCustomAttributes<EventInterceptorAttribute>(false);

                // 逐条包装并添加到 _eventHandlers 集合中
                foreach (var eventSubscribeAttribute in eventSubscribeAttributes)
                {
                    EventHandlerBuilder eventMiddlewareBuilder = new EventHandlerBuilder();
                    foreach (var attribute in eventMiddlewaresAttributes.OrderByDescending(m => m.Order))
                    {
                        attribute.UseEventHandlerDelegate(eventMiddlewareBuilder, serviceProvider);
                    }
                    //使用EndPoint处理器
                    eventMiddlewareBuilder.Use(next => DefaultEndPointEventHandler(next, internalhandler, eventSubscribeAttribute));
                    var handler = eventMiddlewareBuilder.Build();

                    var wrapper = new EventHandlerWrapper(eventSubscribeAttribute.EventName)
                    {
                        Handler = handler,
                        HandlerMethod = eventHandlerMethod,
                        Attribute = eventSubscribeAttribute,
                        Pattern = CheckIsSetFuzzyMatch(eventSubscribeAttribute.FuzzyMatch) ? new Regex(eventSubscribeAttribute.EventName, RegexOptions.Singleline) : default,
                        GCCollect = CheckIsSetGCCollect(eventSubscribeAttribute.GCCollect),
                        Order = eventSubscribeAttribute.Order
                    };

                    _eventHandlers.TryAdd(wrapper, wrapper);
                }
            }
        }
    }
    /// <summary>
    /// 默认终点处理程序
    /// </summary>
    /// <param name="next"></param>
    /// <param name="internalhandler"></param>
    /// <param name="eventSubscribeAttribute"></param>
    /// <returns></returns>
    private EventHandlerDelegate DefaultEndPointEventHandler(EventHandlerDelegate next, Func<EventHandlerExecutingContext, Task> internalhandler, EventDescriptionAttribute eventSubscribeAttribute) => async context =>
    {
        // 判断是否自定义了执行器
        if (Executor == default)
        {
            // 判断是否自定义了重试失败回调服务
            var fallbackRuleService = eventSubscribeAttribute?.FallbackRule == null
                ? null
                : _serviceProvider.GetService(eventSubscribeAttribute.FallbackRule) as IEventFallbackRule;

            // 调用事件处理程序并配置出错执行重试
            await Retry.InvokeAsync(async () =>
            {
                await internalhandler?.Invoke(context);
            }
            , eventSubscribeAttribute?.NumRetries ?? 0
            , eventSubscribeAttribute?.RetryTimeout ?? 1000
            , exceptionTypes: eventSubscribeAttribute?.ExceptionTypes
            , fallbackRule: fallbackRuleService == null ? default : async (ex) => await fallbackRuleService.CallbackAsync(context, ex)
            , retryAction: (total, times) =>
            {
                // 输出重试日志
                AppRealization.Output.Print("EventBus Output", "Retrying {times}/{total} times for {EventName}",
                    Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information, AdditionalParams: new Dictionary<string, object>()
                {
                    {"times",times },
                    {"total",total },
                    {"event-name",eventSubscribeAttribute?.EventName },
                });
            });
        }
        else
        {
            await Executor.ExecuteAsync(context, internalhandler);
        }
    };
    /// <summary>
    /// 检查是否开启模糊匹配事件 Id 功能
    /// </summary>
    /// <param name="fuzzyMatch"></param>
    /// <returns></returns>
    private bool CheckIsSetFuzzyMatch(object fuzzyMatch)
    {
        return fuzzyMatch == null
            ? FuzzyMatch
            : Convert.ToBoolean(fuzzyMatch);
    }

    /// <summary>
    /// 检查是否开启执行完成触发 GC 回收
    /// </summary>
    /// <param name="gcCollect"></param>
    /// <returns></returns>
    private bool CheckIsSetGCCollect(object gcCollect)
    {
        return gcCollect == null
            ? GCCollect
            : Convert.ToBoolean(gcCollect);
    }
}
