using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Core.Standard.Event.Attributes;
using Air.Cloud.Core.Standard.Event.Contexts;

namespace unit.kafaka.client.entry
{
    public class TestMessageSubscriber : IMessageSubscriber
    {
        /// <summary>
        /// 测试订阅1
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [EventDescription("test1")]
        [EventInterceptor(typeof(TestMessageInterceptor))]
        public async Task TestService1(EventHandlerExecutingContext context)
        {
            IEventSource eventSource = context.Source;
            AppRealization.Output.Print("TestMessageSubscriber", "收到消息" + eventSource.Payload.ToString(), AppPrintLevel.Information);
            await Task.CompletedTask;
        }
    }
}
