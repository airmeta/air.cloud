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
namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// <para>zh-cn:应用程序启动配置</para>
    /// <para>en-us:Application startup config</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:通常情况下,我们不建议你配置此选项因为该配置的order配置错误将会导致,继承AppStartup类即可</para>
    /// <para>en-us:Normally, we do not recommend configuring this option as an incorrect order configuration will result. Inheriting the AppStartup class is sufficient</para>
    /// </remarks>
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
        /// 数值越小越先执行该Startup
        /// </summary>
        public int Order { get; set; } = 1000;

        /// <summary>
        /// <para>zh-cn:是否自动加载 默认为true 可以设置为非自动加载</para>
        /// </summary>
        public bool AutoLoad { get; set; } = true;

        public AppStartupAttribute()
        {

        }
        public AppStartupAttribute(int Order)
        {
            this.Order = Order;
        }
    }
}
