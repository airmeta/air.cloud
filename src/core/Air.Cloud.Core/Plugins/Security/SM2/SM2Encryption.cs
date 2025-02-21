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
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

using System.Text;

namespace Air.Cloud.Core.Plugins.Security.SM2
{
    /// <summary>
    /// SM2加密解密操作
    /// </summary>
    /// <remarks>
    /// 加密后返回Base64字符串 解密时需要传入Base64字符串
    /// </remarks>
    public sealed class SM2Encryption
    {
        public static SM2Parameter Parameter;
        static SM2Encryption()
        {
            //初始化基本参数
            Parameter = new SM2Parameter();
        }
        /// <summary>
        /// 椭圆曲线基本域参数
        /// </summary>
        private static readonly X9ECParameters X9ECParameters = GMNamedCurves.GetByName("sm2p256v1");

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="Content">待加密内容</param>
        /// <param name="PublicKey">公钥</param>
        /// <returns>Base64加密结果</returns>
        public static string Encrypt(string Content, string PublicKey)
        {
            byte[] PlainText = Encoding.UTF8.GetBytes(Content);
            //构建引擎  并加密 获取旧标准密文
            byte[] data = Parameter.InitEngine((s) => s.Init(true, new ParametersWithRandom(InitPublicKey(PublicKey))))
                                .ProcessBlock(PlainText, 0, PlainText.Length);
            //调整密文标准到新标准
            return Base64.ToBase64String(ChangeC1C2C3ToC1C3C2(data));
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="Content">密文</param>
        /// <returns></returns>
        public static string Decrypt(string Content, string PrivateKey)
        {
            //调整密文标准
            byte[] plain = ChangeC1C3C2ToC1C2C3(Convert.FromBase64String(Content));
            //构建引擎  并解密 获取原文
            byte[] data = Parameter.InitEngine((s) => s.Init(false, InitPrivateKey(PrivateKey)))
                                 .ProcessBlock(plain, 0, plain.Length);
            return Encoding.UTF8.GetString(data);
        }
        /// <summary>
        /// 获取密钥对
        /// </summary>
        /// <returns>item1:公钥 item2:私钥</returns>
        public static (string, string) GenerateKeyPair()
        {
            AsymmetricCipherKeyPair kPair = genCipherKeyPair();
            ECPrivateKeyParameters ecPrivateKey = (ECPrivateKeyParameters)kPair.Private;
            ECPublicKeyParameters ecPublicKey = (ECPublicKeyParameters)kPair.Public;
            BigInteger priKey = ecPrivateKey.D;
            ECPoint pubKey = ecPublicKey.Q;
            byte[] priByte = priKey.ToByteArray();
            byte[] pubByte = pubKey.GetEncoded(false);
            if (priByte.Length == 33)
            {
                byte[] newPriByte = new byte[32];
                Array.Copy(priByte, 1, newPriByte, 0, 32);
                priByte = newPriByte;
            }
            return (Hex.ToHexString(pubByte), Hex.ToHexString(priByte));
        }

        /// <summary>
        /// 初始化公钥参数
        /// </summary>
        /// <param name="PublicKey">公钥</param>
        /// <returns>公钥参数</returns>
        public static ECPublicKeyParameters InitPublicKey(string PublicKey)
        {
            var PublicHexKey = Hex.Decode(Encoding.UTF8.GetBytes(PublicKey));
            return new ECPublicKeyParameters(Parameter.ecc_curve.DecodePoint(PublicHexKey), Parameter.ecc_bc_spec);
        }
        /// <summary>
        /// 初始化私钥参数
        /// </summary>
        /// <param name="PrivateKey">私钥</param>
        /// <returns>私钥参数</returns>
        public static ECPrivateKeyParameters InitPrivateKey(string PrivateKey)
        {
            var PrivateHexKey = Hex.Decode(Encoding.UTF8.GetBytes(PrivateKey));
            ECPrivateKeyParameters privateKeyParameters = new ECPrivateKeyParameters(new BigInteger(1, PrivateHexKey), Parameter.ecc_bc_spec);
            return privateKeyParameters;
        }

        #region  私有方法 
        /// <summary>
        /// 加解密使用旧标c1||c2||c3，此方法在加密后调用，将结果转化为c1||c3||c2
        /// </summary>
        /// <param name="c1c2c3">旧标准</param>
        /// <returns>C132标准密文</returns>
        private static byte[] ChangeC1C2C3ToC1C3C2(byte[] c1c2c3)
        {
            int c1Len = (X9ECParameters.Curve.FieldSize + 7) / 8 * 2 + 1; //sm2p256v1的这个固定65。可看GMNamedCurves、ECCurve代码。
            const int c3Len = 32;
            byte[] result = new byte[c1c2c3.Length];
            Buffer.BlockCopy(c1c2c3, 0, result, 0, c1Len); //c1
            Buffer.BlockCopy(c1c2c3, c1c2c3.Length - c3Len, result, c1Len, c3Len); //c3
            Buffer.BlockCopy(c1c2c3, c1Len, result, c1Len + c3Len, c1c2c3.Length - c1Len - c3Len); //c2
            return result;
        }

        /// <summary>
        /// 此方法在解密前调用，将密文转化为旧标准c1||c2||c3再去解密
        /// </summary>
        /// <param name="c1c3c2">新标准密文</param>
        /// <returns>C123旧标准密文</returns>
        private static byte[] ChangeC1C3C2ToC1C2C3(byte[] c1c3c2)
        {
            int c1Len = (X9ECParameters.Curve.FieldSize + 7) / 8 * 2 + 1; //sm2p256v1的这个固定65。可看GMNamedCurves、ECCurve代码。
            const int c3Len = 32; //new SM3Digest().GetDigestSize();
            byte[] result = new byte[c1c3c2.Length];
            Buffer.BlockCopy(c1c3c2, 0, result, 0, c1Len); //c1: 0->65
            Buffer.BlockCopy(c1c3c2, c1Len + c3Len, result, c1Len, c1c3c2.Length - c1Len - c3Len); //c2
            Buffer.BlockCopy(c1c3c2, c1Len, result, c1c3c2.Length - c3Len, c3Len); //c3
            return result;
        }

        /// <summary>
        /// 生成密钥对
        /// </summary>
        /// <returns></returns>
        private static AsymmetricCipherKeyPair genCipherKeyPair()
        {
            SM2Parameter ecc_param = new SM2Parameter();
            ECDomainParameters ecDomainParamters = ecc_param.ecc_bc_spec;
            ECKeyGenerationParameters ecGenParam = new ECKeyGenerationParameters(ecDomainParamters, new SecureRandom());

            ECKeyPairGenerator ecKeyPairGenerator = new ECKeyPairGenerator();
            ecKeyPairGenerator.Init(ecGenParam);
            return ecKeyPairGenerator.GenerateKeyPair();
        }
        /// <summary>
        /// SM2参数
        /// </summary>
        public class SM2Parameter
        {
            public static string[] ecc_param = { "FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000FFFFFFFFFFFFFFFF", "FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000FFFFFFFFFFFFFFFC", "28E9FA9E9D9F5E344D5A9E4BCF6509A7F39789F515AB8F92DDBCBD414D940E93", "FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFF7203DF6B21C6052B53BBF40939D54123", "32C4AE2C1F1981195F9904466A39C9948FE30BBFF2660BE1715A4589334C74C7", "BC3736A2F4F6779C59BDCEE36B692153D0A9877CC62A474002DF32E52139F0A0" };
            public BigInteger ecc_p;
            public BigInteger ecc_a;
            public BigInteger ecc_b;
            public BigInteger ecc_n;
            public BigInteger ecc_gx;
            public BigInteger ecc_gy;
            public ECCurve ecc_curve;
            public ECDomainParameters ecc_bc_spec;
            public static SM2Engine Engine;
            public static SM2Signer Signer;
            public SM2Parameter()
            {
                ecc_p = new BigInteger(ecc_param[0], 16);
                ecc_a = new BigInteger(ecc_param[1], 16);
                ecc_b = new BigInteger(ecc_param[2], 16);
                ecc_n = new BigInteger(ecc_param[3], 16);
                ecc_gx = new BigInteger(ecc_param[4], 16);
                ecc_gy = new BigInteger(ecc_param[5], 16);
                ecc_curve = new FpCurve(ecc_p, ecc_a, ecc_b, ecc_n, BigInteger.One);
                ecc_bc_spec = new ECDomainParameters(ecc_curve, ecc_curve.CreatePoint(ecc_gx, ecc_gy), ecc_n);
            }
            /// <summary>
            /// 初始化引擎
            /// </summary>
            /// <param name="Action">初始化动作</param>
            /// <returns>引擎实例</returns>
            public SM2Engine InitEngine(Action<SM2Engine> Action)
            {
                Engine = new SM2Engine();
                Action.Invoke(Engine);
                return Engine;
            }
            public SM2Signer InitSigner(Action<SM2Signer> Action)
            {
                Signer = new SM2Signer();
                Action.Invoke(Signer);
                return Signer;
            }
        }
        #endregion
    }
}