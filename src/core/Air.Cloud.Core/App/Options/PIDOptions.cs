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
using Air.Cloud.Core.Plugins.PID;

namespace Air.Cloud.Core.App.Options
{
    /// <summary>
    /// PID 配置信息
    /// </summary>
    /// <remarks>
    /// 该配置由系统生成,使用当前程序的启动路由地址作为PID的内容主体,
    /// 并进行MD5加密后获得PID值,该值在当前程序的生命周期内不会改变
    /// </remarks>
    public class PIDOptions
    {
        /// <summary>
        /// PID实例
        /// </summary>
        public static PIDOptions Instance { get; set; }
        /// <summary>
        /// PID
        /// </summary>
        public string PID
        {
            get
            {
                return PIDProvider.GetPID();
            }
        }
        /// <summary>
        /// PID内容
        /// </summary>
        public string Content => PIDProvider.StartPath;

        /// <summary>
        /// 初始化PID实例
        /// </summary>
        static PIDOptions()
        {
            Instance = new PIDOptions();
        }
    }
}
