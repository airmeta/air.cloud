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
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Core.Plugins.Security.AES;

/// <summary>
/// AES 加解密
/// </summary>
public class AESEncryption : IPlugin
{
    public const string AES_KEY = "AppSecurity:AESCertInfo:Key";
    public const string AES_IV = "AppSecurity:AESCertInfo:Iv";
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="text">加密文本</param>
    /// <param name="skey">密钥</param>
    /// <returns></returns>
    public static string Encrypt(string Text, string Key = null, string Iv = null)
    {
        if (Iv.IsNullOrEmpty()) Iv = AppConfiguration.Configuration[AES_IV];
        if (Key.IsNullOrEmpty()) Key = AppConfiguration.Configuration[AES_KEY];
        using var aesAlg = Aes.Create();
        if (Key.IsNullOrEmpty()) throw new Exception($"未配置AES密钥[{AES_KEY}]");
        var iv = Iv.IsNullOrEmpty() ? aesAlg.IV : Encoding.UTF8.GetBytes(Iv);
        var encryptKey = Encoding.UTF8.GetBytes(Key);
        using var encryptor = aesAlg.CreateEncryptor(encryptKey, iv);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write, true))
        using (var swEncrypt = new StreamWriter(csEncrypt, leaveOpen: true)) swEncrypt.Write(Text);
        var dataLength = iv.Length + (int)msEncrypt.Length;
        var decryptedContent = msEncrypt.GetBuffer();
        var base64Length = Base64.GetMaxEncodedToUtf8Length(dataLength);
        var result = new byte[base64Length];
        Unsafe.CopyBlock(ref result[0], ref iv[0], (uint)iv.Length);
        Unsafe.CopyBlock(ref result[iv.Length], ref decryptedContent[0], (uint)msEncrypt.Length);

        Base64.EncodeToUtf8InPlace(result, dataLength, out base64Length);

        return Encoding.ASCII.GetString(result.AsSpan()[..base64Length]);
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="hash">加密后字符串</param>
    /// <param name="skey">密钥</param>
    /// <returns></returns>
    public static string Decrypt(string Hash, string Key = null, string Iv = null)
    {
        var fullCipher = Convert.FromBase64String(Hash);
        if (Iv.IsNullOrEmpty()) Iv = AppConfiguration.Configuration[AES_IV];
        if (Key.IsNullOrEmpty()) Key = AppConfiguration.Configuration[AES_KEY];
        using var aesAlg = Aes.Create();
        if (Key.IsNullOrEmpty()) throw new Exception($"未配置AES密钥[{AES_KEY}]");
        var iv = Iv.IsNullOrEmpty() ? aesAlg.IV : Encoding.UTF8.GetBytes(Iv);
        var cipher = new byte[fullCipher.Length - iv.Length];
        Unsafe.CopyBlock(ref iv[0], ref fullCipher[0], (uint)iv.Length);
        Unsafe.CopyBlock(ref cipher[0], ref fullCipher[iv.Length], (uint)(fullCipher.Length - iv.Length));
        var decryptKey = Encoding.UTF8.GetBytes(Key);


        using var decryptor = aesAlg.CreateDecryptor(decryptKey, iv);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}