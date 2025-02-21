using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Standard.Print;

using System.Reflection;

namespace unit.webapp.entry.AspectDependencies
{
    public class DefaultOutputAspect : IAspectAroundHandler
    {
        #region 环绕
        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改入参)", AppRealization.JSON.Serialize(args)));
            if (methodInfo.Name == "PerformService")
            {
                Console.WriteLine("环绕之前修改参数,将[" + args[0].ToString() + "]改成了[李四]");
                args[0] = "李四";
            }
            return args;
        }

        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改响应)", AppRealization.JSON.Serialize(args)));
            if (methodInfo.Name == "PerformService")
            {
                Console.WriteLine("环绕之后修改参数,将返回值[" + result.ToString() + "]改成了[王五]");
                result = "王五";
            }
            return result;
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
            if (methodInfo.Name == "PerformService")
            {
                Console.WriteLine("环绕之前修改参数,将[" + args[0].ToString() + "]改成了[李四1]");
                args[0] = "李四1";
            }
            return args;
        }

        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            AppRealization.Output.Print(new AppPrintInformation("环绕之前切入(可修改响应)", AppRealization.JSON.Serialize(args)));
            if (methodInfo.Name == "PerformService")
            {
                Console.WriteLine("环绕之后修改参数,将返回值[" + result.ToString() + "]改成了[王五1]");
                result = "王五1";
            }
            return result;
        }
        #endregion
    }
}
