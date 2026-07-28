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

namespace Air.Cloud.Modules.SkyMirrorShield.Options
{
    /// <summary>
    /// <para>zh-cn:天镜盾请求运行日志配置。该配置只控制 SkyMirrorShield 中间件直接输出的请求校验日志，不影响 TraceLog 审计日志，也不影响 Warning 及以上的框架异常日志。</para>
    /// <para>en-us:SkyMirrorShield request runtime log options. These options only control request validation logs written directly by the SkyMirrorShield middleware, and do not affect TraceLog audit logs or framework warning-and-above logs.</para>
    /// </summary>
    public sealed class SkyMirrorShieldRequestLogOptions
    {
        /// <summary>
        /// <para>zh-cn:是否启用天镜盾请求运行日志。默认启用，用于兼容既有输出行为；关闭后放行和拒绝请求的天镜盾运行日志都不会输出。</para>
        /// <para>en-us:Whether SkyMirrorShield request runtime logs are enabled. The default is enabled for backward compatibility; when disabled, both allowed-request and rejected-request SkyMirrorShield runtime logs are suppressed.</para>
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否输出已放行请求的运行日志。默认启用；高并发网关可关闭该项，只保留 TraceLog 审计和拒绝日志。</para>
        /// <para>en-us:Whether allowed-request runtime logs are written. The default is enabled; high-concurrency gateways can disable it while keeping TraceLog audit logs and rejected-request logs.</para>
        /// </summary>
        public bool LogAllowedRequests { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否输出被拒绝请求的运行日志。默认启用，用于保留安全拒绝原因。</para>
        /// <para>en-us:Whether rejected-request runtime logs are written. The default is enabled so security rejection reasons remain visible.</para>
        /// </summary>
        public bool LogRejectedRequests { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:已放行请求采样比例。值为 0 表示不采样；值为 N 表示在 `LogAllowedRequests=false` 时大约每 N 次放行请求输出一次运行日志。</para>
        /// <para>en-us:Allowed-request sample rate. A value of 0 disables sampling; a value of N writes roughly one allowed-request runtime log every N allowed requests when `LogAllowedRequests=false`.</para>
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// <para>zh-cn:标准化配置值。采样比例小于 0 时会归零，避免错误配置导致每次请求都参与异常分支。</para>
        /// <para>en-us:Normalizes option values. Negative sample rates are reset to zero so invalid configuration does not push every request through an exceptional branch.</para>
        /// </summary>
        public void Normalize()
        {
            if (SampleRate < 0)
            {
                SampleRate = 0;
            }
        }
    }
}
