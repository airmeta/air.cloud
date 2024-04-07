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
namespace Air.Cloud.Core.App.Loader
{
    /// <summary>
    /// 应用程序启动配置
    /// </summary>
    public class StartupAttribute : Attribute
    {
        /// <summary>
        /// 应用程序唯一编号
        /// </summary>
        public string AppUniqueKey { get; set; }
        /// <summary>
        /// 应用程序名称 
        /// </summary>
        /// <remarks>
        /// 不设置则会自动获取
        /// </remarks>
        public string AppName { get; set; }
        /// <summary>
        /// 应用程序版本
        /// </summary>
        public string AppVersion { get; set; } = "v1.00.00.000";
        /// <summary>
        /// 应用程序总路由地址
        /// </summary>
        public string AppRoute { get; set; } = "/";
        /// <summary>
        /// 应用程序作者
        /// </summary>
        public string AppAuther { get; set; }
        /// <summary>
        /// 越大越往前
        /// </summary>
        public int Order { get; set; }
        ///// <summary>
        ///// 依赖类库,多个类库用逗号隔开  暂时不加 有其他解决方案
        ///// </summary>
        ///// <remarks>
        ///// </remarks>
        //public string DependencyAssembly { get; set; }

    }
}
