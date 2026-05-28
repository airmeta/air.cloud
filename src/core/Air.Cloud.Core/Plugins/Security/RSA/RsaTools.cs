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
    /// RAS 工具
    /// </summary>
    public class RsaTools
    {
        /// <summary>
        /// RSA密钥模型
        /// </summary>
        public class RSAKey
        {
            /// <summary>
            /// 公钥
            /// </summary>
            public string PublicKey { get; set; }
            /// <summary>
            /// 私钥
            /// </summary>
            public string PrivateKey { get; set; }
        }
        /// <summary>
        /// 加密结果
        /// </summary>
        public class RSAEncryptResult
        {
            public bool State { get; set; } = false;
            public string Result { get; set; } = string.Empty;
        }
        /// <summary>
        /// 解密
        /// </summary>
        public class RSADecryptResult : RSAEncryptResult { }

        /// <summary>
        /// 创建RSAKEY
        /// </summary>
        /// <returns></returns>
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
