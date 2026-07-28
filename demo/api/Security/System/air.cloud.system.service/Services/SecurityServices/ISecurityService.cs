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
namespace air.cloud.system.service.Services.SecurityServices
{
    public interface ISecurityService
    {
        //此接口应该包含OAUTH2.0相关的方法定义 比如: 获取授权码、交换令牌、刷新令牌、退出登录、权限验证等

        public string GetAuthorizationCode(string clientId, string redirectUri, string scope, string state);

        public string ExchangeToken(string authorizationCode, string clientId, string clientSecret, string redirectUri);

        public string RefreshToken(string refreshToken, string clientId, string clientSecret);      

        public void RevokeToken(string token, string clientId, string clientSecret);

        public bool ValidateToken(string token, string requiredScope);

        public string GetUserInfo(string token);

    }
}
