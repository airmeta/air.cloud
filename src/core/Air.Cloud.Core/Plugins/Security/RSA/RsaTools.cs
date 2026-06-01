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
namespace Air.Cloud.Core.Plugins.Security.RSA
{
    /// <summary>
    /// <para>zh-cn:提供 RSA 密钥相关工具方法。</para>
    /// <para>en-us:Provides utility methods related to RSA keys.</para>
    /// </summary>
    public class RsaTools
    {
        /// <summary>
        /// <para>zh-cn:表示 RSA 公钥和私钥数据。</para>
        /// <para>en-us:Represents RSA public and private key data.</para>
        /// </summary>
        public class RSAKey
        {
            /// <summary>
            /// <para>zh-cn:获取或设置 RSA 公钥 PEM 文本。</para>
            /// <para>en-us:Gets or sets the RSA public key PEM text.</para>
            /// </summary>
            public string PublicKey { get; set; }
            /// <summary>
            /// <para>zh-cn:获取或设置 RSA 私钥 PEM 文本。</para>
            /// <para>en-us:Gets or sets the RSA private key PEM text.</para>
            /// </summary>
            public string PrivateKey { get; set; }
        }
        /// <summary>
        /// <para>zh-cn:表示 RSA 加密操作结果。</para>
        /// <para>en-us:Represents the result of an RSA encryption operation.</para>
        /// </summary>
        public class RSAEncryptResult
        {
            /// <summary>
            /// <para>zh-cn:获取或设置操作是否成功。</para>
            /// <para>en-us:Gets or sets whether the operation succeeded.</para>
            /// </summary>
            public bool State { get; set; } = false;
            /// <summary>
            /// <para>zh-cn:获取或设置操作结果文本。</para>
            /// <para>en-us:Gets or sets the operation result text.</para>
            /// </summary>
            public string Result { get; set; } = string.Empty;
        }
        /// <summary>
        /// <para>zh-cn:表示 RSA 解密操作结果。</para>
        /// <para>en-us:Represents the result of an RSA decryption operation.</para>
        /// </summary>
        public class RSADecryptResult : RSAEncryptResult { }

        /// <summary>
        /// <para>zh-cn:创建新的 2048 位 RSA 密钥对。</para>
        /// <para>en-us:Creates a new 2048-bit RSA key pair.</para>
        /// </summary>
        /// <returns><para>zh-cn:包含公钥和私钥 PEM 文本的 RSA 密钥模型。</para><para>en-us:An RSA key model containing public and private key PEM text.</para></returns>
        public static RSAKey CreateRSAKey()
        {
            using var rsa = global::System.Security.Cryptography.RSA.Create(2048);
            return new RSAKey()
            {
                PublicKey = NormalizePem(rsa.ExportSubjectPublicKeyInfoPem()),
                PrivateKey = NormalizePem(rsa.ExportRSAPrivateKeyPem())
            };
        }

        private static string NormalizePem(string pem)
        {
            return pem.Replace("\r\n", "\n").Replace("\n", "\r\n\t\t");
        }

    }
}
