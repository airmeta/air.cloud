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
    /// <para>zh-cn:RSA加密</para>
    /// <para>en-us:RSA Encryption Tools</para>
    /// </summary>
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

            using var rsa = System.Security.Cryptography.RSA.Create();
            ImportKey(rsa, pubkey, prikey);

            var cipherBytes = Convert.FromBase64String(data);
            var keySizeBytes = rsa.KeySize / 8;
            if (cipherBytes.Length < keySizeBytes)
            {
                var paddedCipherBytes = new byte[keySizeBytes];
                Buffer.BlockCopy(cipherBytes, 0, paddedCipherBytes, keySizeBytes - cipherBytes.Length, cipherBytes.Length);
                cipherBytes = paddedCipherBytes;
            }

            var plainBytes = rsa.Decrypt(cipherBytes, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        ///  加密
        /// </summary>
        /// <param name="data">原文</param>
        /// <param name="pubkey">公钥</param>
        /// <param name="prikey">私钥</param>
        /// <returns>密文</returns>
        public static string Encrypt(string data, string pubkey, string prikey=null)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            using var rsa = System.Security.Cryptography.RSA.Create();
            ImportKey(rsa, pubkey, prikey);

            var plainBytes = System.Text.Encoding.UTF8.GetBytes(data);
            var encryptBytes = rsa.Encrypt(plainBytes, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptBytes);
        }

        private static void ImportKey(System.Security.Cryptography.RSA rsa, string pubkey, string prikey)
        {
            if (!string.IsNullOrWhiteSpace(prikey))
            {
                rsa.ImportFromPem(prikey);
                return;
            }

            if (!string.IsNullOrWhiteSpace(pubkey))
            {
                rsa.ImportFromPem(pubkey);
                return;
            }

            throw new ArgumentException("RSA公钥或私钥不能为空");
        }

    }
}
