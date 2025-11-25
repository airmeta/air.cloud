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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Core.Standard.Authentication
{
    public interface ISecurityHandlerStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:身份认证方案名称</para>
        /// <para>en-us:Authentication Scheme Name</para>
        /// </summary>
        public static string AuthenticationSchemeName = "AirCloud.Authentication";
        /// <summary>
        /// <para>zh-cn:设置声明主体</para>
        /// <para>en-us:Set claims principal</para>
        /// </summary>
        /// <param name="context">
        ///  <para>zh-cn:HTTP上下文</para>
        ///  <para>en-us:HTTP context</para>
        /// </param>
        /// <param name="claims">
        ///  <para>zh-cn:声明字典</para>
        ///  <para>en-us:Claims dictionary</para>
        /// </param>
        /// <returns></returns>
        public AuthenticationTicket GetAuthenticationTicket(HttpContext context, string SchemeName, IDictionary<string, string> claims);

        /// <summary>
        /// 验证授权
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public AuthenticateResult VerifyAuthorization(HttpContext Context);
    }
}
