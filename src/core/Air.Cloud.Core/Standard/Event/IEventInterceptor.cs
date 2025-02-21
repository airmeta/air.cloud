using Air.Cloud.Core.Standard.Event.Contexts;

namespace Air.Cloud.Core.Standard.Event;
/// <summary>
/// 事件拦截器
/// </summary>
public interface IEventInterceptor
{
    Task ExcuteAsync(EventHandlerExecutingContext context);
}
