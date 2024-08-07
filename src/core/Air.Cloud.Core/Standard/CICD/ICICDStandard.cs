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
using Air.Cloud.Core.Standard.AppResult;
using Air.Cloud.Core.Standard.CICD.Config;
using Air.Cloud.Core.Standard.CICD.Model;

namespace Air.Cloud.Core.Standard.CICD
{
    /// <summary>
    /// CICD约定
    /// </summary>
    /// <remarks>
    /// 包含 安装、启动、停止、卸载、升级、回滚、配置、状态 方法
    /// </remarks>
    public interface ICICDStandard : IStandard
    {
        /// <summary>
        /// 安装
        /// </summary>
        /// <param name="package">程序包信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Install(IPackageInfo package);
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="containerInfo">容器信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Start(IContainerInfo containerInfo);
        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="containerInfo">容器信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Stop(IContainerInfo containerInfo);
        /// <summary>
        /// 卸载
        /// </summary>
        /// <returns></returns>
        IRESTfulResultStandard Uninstall(IContainerInfo containerInfo);
        /// <summary>
        /// 升级
        /// </summary>
        /// <param name="containerInfo">原来的容器信息</param>
        /// <param name="package">新的版本信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Upgrade(IContainerInfo containerInfo, IPackageInfo package);
        /// <summary>
        /// 回滚到上一个版本
        /// </summary>
        /// <param name="containerInfo">容器信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Rollback(IContainerInfo containerInfo);
        /// <summary>
        /// 回滚到某一个版本
        /// </summary>
        /// <param name="containerInfo">容器信息</param>
        /// <param name="package">指定版本程序包信息</param>
        /// <returns></returns>
        IRESTfulResultStandard Rollback(IContainerInfo containerInfo, IPackageInfo package);
        /// <summary>
        /// 配置信息
        /// </summary>
        /// <returns></returns>
        IRESTfulResultStandard Config(IContainerConfig containerConfig);
        /// <summary>
        /// 状态
        /// </summary>
        /// <param name="containerInfo"></param>
        /// <returns></returns>
        IContainerStatus Status(IContainerInfo containerInfo);
    }
}
