using Air.Cloud.Core.Standard.JinYiWei;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    /// <summary>
    /// <para>zh-cn:默认的日志追踪实现(好吧,其实就是没有实现)</para>
    ///  <para>en-us:default trace log dependency</para>
    /// </summary>
    public class DefaultTraceLogDependency : ITraceLogStandard
    {
        public void AddLog(string logContent)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = logContent,
                Title = "default trace log output events"
            });
        }
        public void AddLog(object logContent)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = AppRealization.JSON.Serialize(logContent),
                Title = "default trace log output events"
            });
        }
    }
}
