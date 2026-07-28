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
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.auth.service.Services.AuthStoreServices
{
    /// <summary>
    /// <para>zh-cn:认证存储服务</para>
    /// <para>en-us:Authentication Storage Service</para>
    /// </summary>
    public interface IAuthStoreService:IDynamicService,ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建临时票据</para>
        /// <para>en-us:Create a temporary ticket</para>
        /// </summary>
        /// <returns></returns>
        Task<TicketCreateResult> CreateTempTicketAsync();
        /// <summary>
        /// <para>zh-cn:通过指定票据创建衍生票据</para>
        /// <para>en-us:Create derived tickets through specified tickets</para>
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        Task<TicketCreateResult> ForkAppTicketByTicket(string Ticket);
        /// <summary>
        /// <para>zh-cn:获取指定票据的载荷信息</para>
        /// <para>en-us:Get the payload information of the specified ticket</para>
        /// </summary>
        /// <param name="Ticket"></param>
        /// <returns></returns>
        Task<UserAccountFactory> GetAccountPayloadInfoAsync(string Ticket);

        /// <summary>
        /// <para>zh-cn:创建票据</para>
        /// <para>en-us:Create Ticket</para>
        /// </summary>
        /// <param name="userAccountFactory"></param>
        /// <returns></returns>
        Task<TicketCreateResult> TicketCreateAsync(UserAccountFactory userAccountFactory);

        /// <summary>
        /// <para>zh-cn:清理指定票据对应的登录状态，支持主票据和应用派生票据，并保证重复退出时返回稳定结果。</para>
        /// <para>en-us:Clears the login state for the specified ticket, supports both primary and application fork tickets, and returns a stable result for repeated logout calls.</para>
        /// </summary>
        /// <param name="Ticket">
        /// <para>zh-cn:需要清理的主票据或应用派生票据。</para>
        /// <para>en-us:The primary ticket or application fork ticket to clear.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:票据清理结果。</para>
        /// <para>en-us:The ticket clearing result.</para>
        /// </returns>
        Task<TicketCreateResult> ClearAppTicketByTicket(string Ticket);

    }
}
