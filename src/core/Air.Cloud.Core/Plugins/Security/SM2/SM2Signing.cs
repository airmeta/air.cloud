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
using Org.BouncyCastle.Utilities.Encoders;

using System.Text;

namespace Air.Cloud.Core.Plugins.Security.SM2
{
    /// <summary>
    /// <para>zh-cn:SM2 签名与验签工具。</para>
    /// <para>en-us:Provides SM2 signature creation and verification helpers.</para>
    /// </summary>
    public class SM2Signing
    {
        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="Content">
        /// <para>zh-cn:待签名的原始内容。</para>
        /// <para>en-us:Original content to be signed.</para>
        /// </param>
        /// <param name="PrivateKey">
        /// <para>zh-cn:用于生成签名的 SM2 私钥。</para>
        /// <para>en-us:SM2 private key used to generate the signature.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:十六进制格式的签名结果。</para>
        /// <para>en-us:Signature result encoded as a hexadecimal string.</para>
        /// </returns>
        public static string Sign(string Content, string PrivateKey)
        {
            byte[] Signature = SM2Encryption.Parameter.InitSigner((s) =>
            {
                byte[] Msg = Encoding.UTF8.GetBytes(Content);
                s.Init(true, SM2Encryption.InitPrivateKey(PrivateKey));
                s.BlockUpdate(Msg, 0, Msg.Length);
            }).GenerateSignature();
            return Hex.ToHexString(Signature);
        }
        /// <summary>
        /// 验签
        /// </summary>
        /// <param name="Content">
        /// <para>zh-cn:用于验签的原始内容。</para>
        /// <para>en-us:Original content used for signature verification.</para>
        /// </param>
        /// <param name="Sign">
        /// <para>zh-cn:需要验证的十六进制签名。</para>
        /// <para>en-us:Hexadecimal signature to verify.</para>
        /// </param>
        /// <param name="PublicKey">
        /// <para>zh-cn:用于验签的 SM2 公钥。</para>
        /// <para>en-us:SM2 public key used to verify the signature.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:签名有效返回 true，否则返回 false。</para>
        /// <para>en-us:Returns true when the signature is valid; otherwise false.</para>
        /// </returns>
        public static bool VerifySign(string Content, string Sign, string PublicKey)
        {
            byte[] signHex = Hex.Decode(Sign);
            bool Result = SM2Encryption.Parameter.InitSigner(s =>
             {
                 byte[] msgByte = Encoding.UTF8.GetBytes(Content);
                 s.Init(false, SM2Encryption.InitPublicKey(PublicKey));
                 s.BlockUpdate(msgByte, 0, msgByte.Length);
             }).VerifySignature(signHex);
            return Result;
        }
    }
}
