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
namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// 应用程序启动配置
    /// </summary>
    [IgnoreScanning, AttributeUsage(AttributeTargets.Class)]
    public class AppStartupAttribute : Attribute
    {
        /// <summary>
        /// 应用程序唯一编号
        /// </summary>
        public string AppKey { get; set; }
        /// <summary>
        /// 应用程序名称 
        /// </summary>
        public string AppName { get; set; }
        /// <summary>
        /// 应用程序版本
        /// </summary>
        /// <remarks>
        /// 实验性功能,暂未启用
        /// </remarks>
        public string AppVersion { get; set; } = "1.0.0.0";
        /// <summary>
        /// 应用程序路由地址
        /// </summary>
        /// <remarks>
        /// 实验性功能,暂未启用
        /// </remarks>
        public string AppRoute { get; set; } = "/";
        /// <summary>
        /// 应用程序作者
        /// </summary>
        public string AppAuther { get; set; }
        /// <summary>
        /// 数值越大越先执行该Startup
        /// </summary>
        public int Order { get; set; } = 1000;

        public AppStartupAttribute()
        {

        }
        public AppStartupAttribute(int Order)
        {
            this.Order = Order;
        }
    }
}
