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

namespace Air.Cloud.Core.Standard.ServerCenter
{
    /// <summary>
    /// 服务中心标准
    /// </summary>
    public  interface IServerCenterStandard
    {
        /// <summary>
        /// 查询服务信息
        /// </summary>
        /// <typeparam name="T">服务信息类型</typeparam>
        /// <returns>所有服务信息</returns>
        Task<IList<T>> Query<T>() where T : IServerCenterServiceOptions, new();
        /// <summary>
        /// 根据服务标识获取某个服务信息
        /// </summary>
        /// <param name="Key">服务标识</param>
        /// <returns></returns>
        Task<object> Get(string Key);
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务信息类型</typeparam>
        /// <param name="serverCenterServiceInformation">服务信息</param>
        /// <returns>是否成功</returns>
        Task<bool> Register<T>(T serverCenterServiceInformation) where T : class, IServerCenterServiceRegisterOptions, new();
    }
}
