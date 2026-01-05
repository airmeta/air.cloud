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
namespace air.gateway.Options
{
    public sealed class TraceLogSettings
    {
        /// <summary>
        /// 关键词过滤
        /// </summary>
        public IList<string> Filters { get; set; }=new List<string>();
        /// <summary>
        /// 是否启用本地记录
        /// </summary>
        public bool? EnableLocalLog { get; set; } = false;
        /// <summary>
        /// 本地记录目录
        /// </summary>
        public string LocalLogDirectory { get; set; } = "logs";

        internal static object SetDefaultSettings(TraceLogSettings options)
        {
            options.Filters = options.Filters.Count>0?options.Filters:new List<string>();
            options.EnableLocalLog ??= false;
            options.LocalLogDirectory ??= "logs";
            return options;
        }
    }
}
