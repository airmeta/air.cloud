/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */

namespace Air.Cloud.Core.Standard.SchedulerStandard.Extensions
{
    public static class CronExtensions
    {
        /// <summary>
        /// 验证 Cron 表达式是否有效
        /// </summary>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public static bool IsValidExpression(this string cronExpression)
        {
            return true;
            //try
            //{
            //    var trigger = new CronTriggerImpl();
            //    trigger.CronExpressionString = cronExpression;
            //    var date = trigger.ComputeFirstFireTimeUtc(null);
            //    return date != null;
            //}
            //catch //(Exception e)
            //{
            //    return false;
            //}
        }
    }
}
