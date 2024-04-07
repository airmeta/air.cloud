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

namespace Air.Cloud.Core.Plugins.Security.SM2
{
    public class SM2Signing
    {
        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="data">待签名</param>
        /// <param name="PrivateKey">私钥</param>
        /// <returns></returns>
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
        /// <param name="data"></param>
        /// <param name="sign"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
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