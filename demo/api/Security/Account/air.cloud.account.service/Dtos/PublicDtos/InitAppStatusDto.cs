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
using air.cloud.security.common.Enums;

namespace air.cloud.account.service.Dtos.PublicDtos
{

    /// <summary>
    /// <para>zh-cn:应用状态传输对象</para>
    /// <para>en-us:Application Status Transfer Object</para>
    /// </summary>
    public class AppStatusDto
    {
        /// <summary>
        /// 是否具有应用
        /// </summary>
        public bool HasApp { get; set; } = true;
        /// <summary>
        /// 唯一应用标识
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// 客户端信息
        /// </summary>
        public ClientConfig Client { get; set; }

        /// <summary>
        /// 设置项
        /// </summary>
        public IDictionary<string, string> Settings { get; set; }

        /// <summary>
        /// <para>zh-cn:登录页地址</para>
        /// <para>en-us:Login page path</para>
        /// </summary>
        public string LoginPath { get; set; }

    }

    public class ClientConfig
    {
        /// <summary>
        /// 是否启用多因素认证
        /// </summary>
        public IsOrNotEnum EnableMFA { get; set; } = IsOrNotEnum.否;
        /// <summary>
        /// 票据信息
        /// </summary>
        public string Ticket { get; set; }
        /// <summary>
        /// 超时时间(秒)
        /// </summary>
        public int ExpiredSeconds { get; set; }
        /// <summary>
        ///  加密方式
        /// </summary>
        public AppEntryptTypeEnum AppEntryptType { get; set; }
        /// <summary>
        /// 私钥(用于对方解密我方传输出去的数据)
        /// </summary>
        public string PrivateKey { get; set; }

    }
}
