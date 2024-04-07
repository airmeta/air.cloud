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
using System.Security.Cryptography;
using System.Text;

using XC.RSAUtil;

namespace Air.Cloud.Core.Plugins.Security.RSA
{
    public class RsaEncryption : RsaTools
    {
        /// <summary>
        ///  解密
        /// </summary>
        /// <param name="data">密文</param>
        /// <param name="pubkey">公钥</param>
        /// <param name="prikey">私钥</param>
        /// <returns>原文</returns>
        public static string Decrypt(string data, string pubkey, string prikey)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            var rsaPkcs1Util = new RsaPkcs1Util(Encoding.UTF8, pubkey, prikey, 2048);
            var content = rsaPkcs1Util.Decrypt(data, RSAEncryptionPadding.Pkcs1);
            return content;
        }

        /// <summary>
        ///  加密
        /// </summary>
        /// <param name="data">原文</param>
        /// <param name="pubkey">公钥</param>
        /// <param name="prikey">私钥</param>
        /// <returns>密文</returns>
        public static string Encrypt(string data, string pubkey, string prikey)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            RsaPkcs1Util rsaPkcs1Util = new RsaPkcs1Util(Encoding.UTF8, pubkey, prikey);
            var encrypt = rsaPkcs1Util.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            return encrypt;
        }

    }
}