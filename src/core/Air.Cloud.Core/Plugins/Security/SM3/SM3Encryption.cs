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
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;

using System.Text;

namespace Air.Cloud.Core.Plugins.Security.SM3
{
    /// <summary>
    /// SM3加密操作
    /// </summary>
    /// <remarks>
    /// 加密后返回普通字符串
    /// </remarks>
    public sealed class SM3Encryption
    {
        /// <summary>
        /// 使用自定义密钥加密
        /// </summary>
        /// <param name="data"></param>
        /// <returns>16进制字符串</returns>
        public static string Entrypt(string data, string key)
        {
            byte[] msg1 = Encoding.UTF8.GetBytes(data);
            byte[] key1 = Encoding.UTF8.GetBytes(key);
            KeyParameter keyParameter = new KeyParameter(key1);
            SM3Digest sm3 = new SM3Digest();
            HMac mac = new HMac(sm3);//带密钥的杂凑算法
            mac.Init(keyParameter);
            mac.BlockUpdate(msg1, 0, msg1.Length);
            byte[] md = new byte[mac.GetMacSize()];
            mac.DoFinal(md, 0);
            return Encoding.UTF8.GetString(Hex.Encode(md));
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data"></param>
        /// <returns>16进制字符串</returns>
        public static string Entrypt(string data)
        {
            var msg = Encoding.UTF8.GetBytes(data);
            SM3Digest sm3 = new SM3Digest();
            sm3.BlockUpdate(msg, 0, msg.Length);
            byte[] md = new byte[sm3.GetDigestSize()];//SM3算法产生的哈希值大小
            sm3.DoFinal(md, 0);
            return Encoding.UTF8.GetString(Hex.Encode(md));
        }
    }
}
