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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace air.cloud.security.common.Model.AppClientInfos
{
    public interface IAppClientInfo
    {
        /// <summary>
        /// 获取指定的客户端请求头
        /// </summary>
        /// <param name="headerKey"></param>
        /// <returns></returns>
        public string GetClientProperty(string headerKey);

        /// <summary>
        /// 获取所有客户端请求头
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetAllClientHeaders();

        /// <summary>
        /// 应用ID头
        /// </summary>
        public const string CLIENT_APPID_HEADER = "AppId";
        /// <summary>
        /// 获取客户端应用ID
        /// </summary>
        /// <returns></returns>
        public string GetClientAppId();

        /// <summary>
        /// 身份令牌头
        /// </summary>
        public const string CLIENT_Ticket_HEADER = "Ticket";
        /// <summary>
        /// 获取客户端Ticket
        /// </summary>
        /// <returns></returns>
        public string GetClientTicket();

        /// <summary>
        /// 签名头
        /// </summary>
        public const string CLIENT_SIGN_HEADER = "Sign";

        /// <summary>
        /// 获取客户端签名
        /// </summary>
        /// <returns></returns>
        public string GetClientSign();

        /// <summary>
        /// 时间戳头
        /// </summary>
        public const string CLIENT_TIMESTAMP_HEADER = "TimeStamp";

        /// <summary>
        /// 获取客户端时间戳
        /// </summary>
        /// <returns></returns>
        public string GetClientTimeStamp();

        /// <summary>
        /// 客户端随机值
        /// </summary>
        public const string CLIENT_NONCE_HEADER = "Nonce";

        /// <summary>
        /// 获取客户端随机数
        /// </summary>
        /// <returns></returns>
        public string GetClientNonce();

    }
}
