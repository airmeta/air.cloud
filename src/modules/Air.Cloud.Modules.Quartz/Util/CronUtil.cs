using Quartz.Impl.Triggers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.Quartz.Util
{
    /// <summary>
    /// <para>zh-cn:Cron表达式工具类</para>
    /// <para>en-us:Cron expression util</para>
    /// </summary>
    public static  class CronUtil
    {
        /// <summary>
        /// 验证 Cron 表达式是否有效
        /// </summary>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public static bool IsValidExpression(this string cronExpression)
        {
            try
            {
                var trigger = new CronTriggerImpl();
                trigger.CronExpressionString = cronExpression;
                var date = trigger.ComputeFirstFireTimeUtc(null);
                return date != null;
            }
            catch //(Exception e)
            {
                return false;
            }
        }
    }
}
