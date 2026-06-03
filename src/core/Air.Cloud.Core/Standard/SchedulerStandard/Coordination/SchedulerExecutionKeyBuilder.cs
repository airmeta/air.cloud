/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
namespace Air.Cloud.Core.Standard.SchedulerStandard.Coordination
{
    /// <summary>
    /// <para>zh-cn:调度执行唯一键构建器。</para>
    /// <para>en-us:Scheduler execution unique key builder.</para>
    /// </summary>
    public static class SchedulerExecutionKeyBuilder
    {
        /// <summary>
        /// <para>zh-cn:根据唯一策略生成稳定的执行唯一键。</para>
        /// <para>en-us:Builds a stable execution unique key by uniqueness policy.</para>
        /// </summary>
        public static string Build(SchedulerExecutionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.ExecutionKey))
            {
                return context.ExecutionKey;
            }

            var serviceName = Normalize(context.ServiceName, "default-service");
            var groupName = Normalize(context.GroupName, "default-group");
            var jobId = Normalize(context.JobId, context.JobName);

            return context.UniqueMode == SchedulerExecutionUniqueMode.PerJob
                ? $"scheduler:{serviceName}:{groupName}:{jobId}"
                : $"scheduler:{serviceName}:{groupName}:{jobId}:{BuildFireTimeKey(context)}";
        }

        private static string BuildFireTimeKey(SchedulerExecutionContext context)
        {
            return context.ScheduledFireTimeUtc?.UtcDateTime.ToString("yyyyMMddHHmmssfffffff")
                ?? Normalize(context.FireInstanceId, DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfffffff"));
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
