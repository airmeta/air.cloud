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
using Microsoft.AspNetCore.Authorization;
namespace Air.Cloud.Core.Standard.Jwt
{
    /// <summary>
    /// <para>zh-cn:Jwt处理标准</para>
    /// <para>en-us:JwtHandlerStandard</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:该标准定义了JWT验证处理的基本方法，包括验证成功和失败的处理逻辑。不过,在分布式场景下或微服务场景下我们建议使用IAuthenticationStandard相关服务来处理身份认证</para>
    /// </remarks>
    ///
    ///
    public interface IJwtHandlerStandard:IStandard
    {
        /// <summary>
        /// 验证成功
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task AuthorizeHandleAsync(AuthorizationHandlerContext context);

        /// <summary>
        /// 验证失败
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task UnAuthorizeHandleAsync(AuthorizationHandlerContext context);
    }
}
