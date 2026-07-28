/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "Incompatible With Secondary Licenses" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;

namespace air.cloud.gateway.Const
{
    /// <summary>
    /// <para xml:lang="zh-cn">提供网关签名校验使用的 RSA 密钥，并始终从运行时配置读取，避免将私钥写入示例源码。</para>
    /// <para xml:lang="en-us">Provides RSA keys for gateway signature validation and always reads them from runtime configuration so no private key is embedded in the sample source.</para>
    /// </summary>
    public static class RsaKeyConst
    {
        /// <summary>
        /// <para xml:lang="zh-cn">获取配置项 <c>AppSettings:PrivateKey</c> 中的 RSA 私钥。未配置时返回空值，签名校验将无法完成。</para>
        /// <para xml:lang="en-us">Gets the RSA private key from <c>AppSettings:PrivateKey</c>. Returns an empty value when unconfigured, preventing signature validation from succeeding.</para>
        /// </summary>
        public static string PRIVATE_KEY => AppConfigurationLoader.InnerConfiguration["AppSettings:PrivateKey"];

        /// <summary>
        /// <para xml:lang="zh-cn">获取配置项 <c>AppSettings:PublicKey</c> 中的 RSA 公钥。未配置时返回空值，签名校验将无法完成。</para>
        /// <para xml:lang="en-us">Gets the RSA public key from <c>AppSettings:PublicKey</c>. Returns an empty value when unconfigured, preventing signature validation from succeeding.</para>
        /// </summary>
        public static string PUBLIC_KEY => AppConfigurationLoader.InnerConfiguration["AppSettings:PublicKey"];
    }
}
