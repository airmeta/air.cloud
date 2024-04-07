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
using Org.BouncyCastle.Utilities.Encoders;

using System.Text;

namespace Air.Cloud.Core.Plugins.Security.SM4
{
    /// <summary>
    /// SM4加密解密操作
    /// </summary>
    /// <remarks>
    /// 加密结果返回的是16进制字符串
    /// </remarks>
    public sealed class SM4Encryption
    {
        /// <summary>
        /// 加密ECB模式
        /// </summary>
        /// <param name="secretKey">密钥</param>
        /// <param name="plainText">明文</param>
        /// <returns>返回Base64密文</returns>
        public static string EncryptECB(string Content, string SecretKey, SM4ResultMode Mode = SM4ResultMode.BASE64)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] contentBytes = Encoding.UTF8.GetBytes(Content);
            SM4Parameter ctx = new SM4Parameter(SM4Util.SM4_ENCRYPT);
            byte[] encrypted = new SM4Util().sm4_setkey_enc(ctx, keyBytes).sm4_crypt_ecb(ctx, contentBytes);
            return Mode == SM4ResultMode.BASE64 ? Convert.ToBase64String(Hex.Encode(encrypted)) : Encoding.UTF8.GetString(Hex.Encode(encrypted));
        }

        /// <summary>
        /// 加密CBC模式
        /// </summary>
        /// <param name="secretKey">密钥</param>
        /// <param name="iv">向量</param>
        /// <param name="plainText">明文</param>
        /// <returns>返回密文</returns>
        public static string EncryptCBC(string Content, string secretKey, string iv, SM4ResultMode Mode = SM4ResultMode.BASE64)
        {
            SM4Parameter ctx = new SM4Parameter(SM4Util.SM4_ENCRYPT);
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] encrypted = new SM4Util().sm4_setkey_enc(ctx, keyBytes).sm4_crypt_cbc(ctx, ivBytes, Encoding.UTF8.GetBytes(Content));
            byte[] bs = Hex.Encode(encrypted);
            return Mode == SM4ResultMode.BASE64 ? Base64.ToBase64String(bs) : Encoding.UTF8.GetString(bs);
        }

        /// <summary>
        /// 解密ECB模式
        /// </summary>
        /// <param name="secretKey">密钥</param>
        /// <param name="Content">密文</param>
        /// <returns>返回Base64明文</returns>
        public static string DecryptECB(string Content, string SecretKey, SM4ResultMode Mode = SM4ResultMode.BASE64)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] contentBytes = Hex.Decode(Mode == SM4ResultMode.BASE64 ? Convert.FromBase64String(Content) : Encoding.UTF8.GetBytes(Content));
            SM4Parameter ctx = new SM4Parameter(SM4Util.SM4_DECRYPT);
            byte[] decrypted = new SM4Util().sm4_setkey_dec(ctx, keyBytes).sm4_crypt_ecb(ctx, contentBytes);
            return Encoding.UTF8.GetString(decrypted);
        }


        /// <summary>
        /// 解密CBC模式
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="SecretKey"></param>
        /// <param name="Hexstring"></param>
        /// <param name="Iv"></param>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public static string DecryptCBC(string Content, string SecretKey, string Iv, SM4ResultMode Mode = SM4ResultMode.BASE64)
        {
            SM4Parameter ctx = new SM4Parameter(SM4Util.SM4_DECRYPT);
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] ivBytes = Encoding.UTF8.GetBytes(Iv);
            byte[] ContentBytes = Mode == SM4ResultMode.BASE64 ? Convert.FromBase64String(Content) : Encoding.UTF8.GetBytes(Content);
            byte[] bs = Hex.Decode(ContentBytes);
            Console.WriteLine("明文解密结果长度:" + bs.Length);
            byte[] decrypted = new SM4Util().sm4_setkey_dec(ctx, keyBytes).sm4_crypt_cbc(ctx, ivBytes, bs);
            return Encoding.UTF8.GetString(decrypted);
        }
    }

}

