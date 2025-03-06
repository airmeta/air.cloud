using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Standard.Print;

using System.Reflection;

namespace Air.Cloud.Core.Extensions.Aspects
{
    /// <summary>
    /// <para>zh-cn: 网络请求异常环绕</para>
    /// <para>en-us: Network request exception around</para>
    /// </summary>
    public  class IfHttpRequestException : IAspectAroundHandler
    {
        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            return result;
        }

        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            return args;
        }

        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException : Exception, new()
        {
            if(exception is HttpRequestException)
            {
                AppPrintInformation appPrintInformation = new AppPrintInformation("网络请求异常", $"在执行[{methodInfo.DeclaringType}]的方法[{methodInfo.Name}]时出现网络异常", Standard.Print.AppPrintInformation.AppPrintLevel.Error, true, new Dictionary<string, object>()
                {
                    {"error",exception }
                });
                AppRealization.TraceLog.Write(appPrintInformation);
                throw new Exception("网络请求异常");
            }
        }
    }
}
