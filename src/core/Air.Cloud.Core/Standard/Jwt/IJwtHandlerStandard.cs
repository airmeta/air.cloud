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
using Microsoft.AspNetCore.Authorization;
namespace Air.Cloud.Core.Standard.Jwt
{
    public  interface IJwtHandlerStandard
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
