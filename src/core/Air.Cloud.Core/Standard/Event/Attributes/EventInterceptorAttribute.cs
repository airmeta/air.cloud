using Air.Cloud.Core.Standard.Event.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Core.Standard.Event.Attributes;
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EventInterceptorAttribute : Attribute
{
    /// <summary>
    /// 排序规则
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// 事件中间处理程序集合
    /// </summary>
    private Type[] TMiddlewares { get; set; }
    public EventInterceptorAttribute(params Type[] tMiddlewares) : this(1, tMiddlewares)
    { }
    public EventInterceptorAttribute(int order, params Type[] tMiddlewares)
    {
        Order = order;
        TMiddlewares = tMiddlewares;
    }
    /// <summary>
    /// 转换为事件处理器委托
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public  EventHandlerBuilder UseEventHandlerDelegate(EventHandlerBuilder builder, IServiceProvider serviceProvider)
    {
        foreach (var TMiddleware in TMiddlewares)
        {
            //转换为Func<HttpHandlerDelegate, HttpHandlerDelegate>
            Func<EventHandlerDelegate, EventHandlerDelegate> delegates = next =>
            {
                var middleware = ActivatorUtilities.CreateInstance(serviceProvider, TMiddleware, next) as IEventInterceptor;
                // 返回一个新的HttpHandlerDelegate，它委托给middleware的ExcuteAsync方法
                return async context =>
                {
                    await middleware.ExcuteAsync(context);
                };
            };
            builder.Use(delegates);
        }
        return builder;
    }
}
