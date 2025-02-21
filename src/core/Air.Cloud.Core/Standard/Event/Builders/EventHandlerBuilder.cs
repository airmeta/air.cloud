using Air.Cloud.Core.Standard.Event.Contexts;

namespace Air.Cloud.Core.Standard.Event.Builders;
/// <summary>
/// 构建事件处理器
/// </summary>
public class EventHandlerBuilder
{
    private readonly IList<Func<EventHandlerDelegate, EventHandlerDelegate>> _interceptors = new List<Func<EventHandlerDelegate, EventHandlerDelegate>>();

    public EventHandlerDelegate Build()
    {
        EventHandlerDelegate next = context =>
        {
            return Task.CompletedTask;
        };
        foreach (var Interceptor in _interceptors.Reverse())
        {
            next = Interceptor.Invoke(next);
        }
        return next;
    }

    public EventHandlerBuilder Use(Func<EventHandlerDelegate, EventHandlerDelegate> eventMiddleware)
    {
        _interceptors.Add(eventMiddleware);
        return this;
    }
}
/// <summary>
/// 事件处理委托
/// </summary>
/// <param name="context"></param>
/// <returns></returns>
public delegate Task EventHandlerDelegate(EventHandlerExecutingContext context);
