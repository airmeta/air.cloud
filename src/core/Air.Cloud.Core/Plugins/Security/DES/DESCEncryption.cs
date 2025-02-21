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
using Air.Cloud.Core.Plugins.Security.MD5;

using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Core.Plugins.Security.DES;

/// <summary>
/// DESC 加解密
/// </summary>
[IgnoreScanning]
public class DESCEncryption
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="text">加密文本</param>
    /// <param name="skey">密钥</param>
    /// <param name="uppercase">是否输出大写加密，默认 false</param>
    /// <returns></returns>
    public static string Encrypt(string text, string skey, bool uppercase = false)
    {
        using var des = System.Security.Cryptography.DES.Create();
        byte[] inputByteArray;
        inputByteArray = Encoding.Default.GetBytes(text);

        des.Key = Encoding.ASCII.GetBytes(MD5Encryption.GetMd5By8(skey, uppercase));
        des.IV = Encoding.ASCII.GetBytes(MD5Encryption.GetMd5By8(skey, uppercase));

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);

        cs.Write(inputByteArray, 0, inputByteArray.Length);
        cs.FlushFinalBlock();

        var ret = new StringBuilder();
        foreach (var b in ms.ToArray())
        {
            ret.AppendFormat("{0:X2}", b);
        }

        return ret.ToString();
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="hash">加密后字符串</param>
    /// <param name="skey">密钥</param>
    /// <param name="uppercase">是否输出大写加密，默认 false</param>
    /// <returns></returns>
    public static string Decrypt(string hash, string skey, bool uppercase = false)
    {
        using var des = System.Security.Cryptography.DES.Create();
        int len;
        len = hash.Length / 2;
        var inputByteArray = new byte[len];
        int x, i;

        for (x = 0; x < len; x++)
        {
            i = Convert.ToInt32(hash.Substring(x * 2, 2), 16);
            inputByteArray[x] = (byte)i;
        }

        des.Key = Encoding.ASCII.GetBytes(MD5Encryption.GetMd5By8(skey, uppercase));
        des.IV = Encoding.ASCII.GetBytes(MD5Encryption.GetMd5By8(skey, uppercase));

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);

        cs.Write(inputByteArray, 0, inputByteArray.Length);
        cs.FlushFinalBlock();

        return Encoding.Default.GetString(ms.ToArray());
    }
}