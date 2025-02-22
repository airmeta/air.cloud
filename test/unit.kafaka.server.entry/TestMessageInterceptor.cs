using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Core.Standard.Event.Builders;
using Air.Cloud.Core.Standard.Event.Contexts;

using static Air.Cloud.Core.Standard.Print.AppPrintInformation;

namespace unit.kafaka.server.entry
{
    public class TestMessageInterceptor : IEventInterceptor
    {
        private readonly EventHandlerDelegate _next;
        public TestMessageInterceptor(EventHandlerDelegate next)
        {
            _next = next;
        }

        public async Task ExcuteAsync(EventHandlerExecutingContext context)
        {
            //前置执行 todo:
            //....

            AppRealization.Output.Print("TestMessageInterceptor", "收到消息来了...", AppPrintLevel.Information);
            await _next.Invoke(context);
            AppRealization.Output.Print("TestMessageInterceptor", "收到消息离了...", AppPrintLevel.Information);

            //后置执行 todo:
            //....
        }
    }
}
