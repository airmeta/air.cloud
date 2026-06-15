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
namespace Air.Cloud.Modules.RocketMQ.Model
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 客户端基础配置，封装生产者和消费者共同使用的 Endpoint、TLS、超时与凭证。</para>
    /// <para>en-us:Base RocketMQ client configuration that wraps endpoint, TLS, timeout, and credentials shared by producers and consumers.</para>
    /// </summary>
    public class RocketMQClientConfig
    {
        /// <summary>
        /// <para>zh-cn:RocketMQ Proxy Endpoint 地址，例如 127.0.0.1:8081。</para>
        /// <para>en-us:RocketMQ Proxy endpoint, for example 127.0.0.1:8081.</para>
        /// </summary>
        public string Endpoints { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用 TLS/SSL。</para>
        /// <para>en-us:Whether TLS/SSL is enabled.</para>
        /// </summary>
        public bool SslEnabled { get; set; }

        /// <summary>
        /// <para>zh-cn:RocketMQ 客户端请求超时时间；为空时使用 SDK 默认值。</para>
        /// <para>en-us:RocketMQ client request timeout. When null, the SDK default is used.</para>
        /// </summary>
        public TimeSpan? RequestTimeout { get; set; }

        /// <summary>
        /// <para>zh-cn:访问密钥；为空时不设置凭证。</para>
        /// <para>en-us:Access key. When empty, credentials are not configured.</para>
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// <para>zh-cn:访问密钥 Secret；为空时不设置凭证。</para>
        /// <para>en-us:Access secret. When empty, credentials are not configured.</para>
        /// </summary>
        public string AccessSecret { get; set; }

        /// <summary>
        /// <para>zh-cn:临时安全令牌。</para>
        /// <para>en-us:Temporary security token.</para>
        /// </summary>
        public string SecurityToken { get; set; }
    }
}
