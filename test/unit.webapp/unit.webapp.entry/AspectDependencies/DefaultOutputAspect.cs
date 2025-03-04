using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Standard.Print;

using Nest;

using System.Reflection;

namespace unit.webapp.entry.AspectDependencies
{
    public class DefaultOutputAspect : IAspectAroundHandler
    {
        #region 环绕
        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改入参)", AppRealization.JSON.Serialize(args)));
         
            return args;
        }

        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改响应)", AppRealization.JSON.Serialize(args)));
          
            return result;
        }

        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException : Exception, new()
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕时出现异常", AppRealization.JSON.Serialize(new
            {
                MethodInfo = methodInfo.Name,
                Args = args,
                Exception = exception
            })
                           ));
        }
        #endregion
    }


    public class DefaultOutputAspect1 : IAspectAroundHandler, IAspectExecuteHandler
    {
        #region  切面
        public void Execute_Before(MethodInfo methodInfo,object[] args)
        {
            AppRealization.Output.Print(new AppPrintInformation("执行之前切入(不可修改入参)", AppRealization.JSON.Serialize(args)));
        }

        public void Execute_After(MethodInfo methodInfo, object retValue)
        {
            AppRealization.Output.Print(new AppPrintInformation("执行之后切入(不可修改响应)", AppRealization.JSON.Serialize(retValue)));
        }
        #endregion

        #region 环绕
        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改入参)", AppRealization.JSON.Serialize(args)));
            return args;
        }

        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之后切入(可修改响应)", AppRealization.JSON.Serialize(args)));
            return result;
        }
        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException : Exception, new()
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕时出现异常", AppRealization.JSON.Serialize(new
            {
                MethodInfo = methodInfo.Name,
                Args = args,
                Exception = exception
            })
            ));
        }
        #endregion



    }
}
