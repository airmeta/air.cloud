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
using air.cloud.system.model.Dtos.UserAccountLogDtos;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.UserDomains
{
    /// <summary>
    /// <para>zh-cn:用户账户日志领域接口</para>
    /// <para>en-us:User account log domain interface</para>
    /// </summary>
    public interface IUserAccountLogDomain : IEntityDomain, ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建用户账户日志</para>
        /// <para>en-us:Create user account log</para>
        /// </summary>
        /// <param name="log">
        ///  <para>zh-cn:用户账户日志保存传输对象</para>
        ///  <para>en-us:User account log save DTO</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回日志ID</para>
        ///  <para>en-us:Returns log Id</para>
        /// </returns>
        Task<string> CreateUserAccountLogAsync(UserAccountLogSDto log);

        /// <summary>
        /// <para>zh-cn:分页查询用户账户日志</para>
        /// <para>en-us:Query user account logs with paging</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:基础查询参数</para>
        ///  <para>en-us:Basic query parameters</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:分页的日志返回对象列表</para>
        ///  <para>en-us:Paged list of log response DTOs</para>
        /// </returns>
        Task<PageList<UserAccountLogRDto>> QueryUserAccountLogsAsync(UserAccountLogQDto dto);

        /// <summary>
        /// <para>zh-cn:获取用户账户日志详情</para>
        /// <para>en-us:Get user account log detail</para>
        /// </summary>
        /// <param name="id">
        ///  <para>zh-cn:日志ID</para>
        ///  <para>en-us:Log Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:用户账户日志返回传输对象</para>
        ///  <para>en-us:User account log response DTO</para>
        /// </returns>
        Task<UserAccountLogRDto?> GetUserAccountLogAsync(string id);
    }
}