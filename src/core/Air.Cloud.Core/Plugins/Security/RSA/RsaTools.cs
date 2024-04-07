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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

using System.Security.Cryptography;
using System.Text;

using XC.RSAUtil;

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
            var keyList = RsaKeyGenerator.Pkcs1Key(2048, true);
            var privateKey = keyList[0];
            var publicKey = keyList[1];
            return new RSAKey()
            {
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
        }

        #region  转换方法
        /// <summary>
        /// XML公钥转成Pem公钥
        /// </summary>
        /// <param name="xmlPublicKey"></param>
        /// <returns></returns>
        public static string XmlPublicKeyToPem(string xmlPublicKey)
        {
            RSAParameters rsaParam;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(xmlPublicKey);
                rsaParam = rsa.ExportParameters(false);
            }
            RsaKeyParameters param = new RsaKeyParameters(false, new BigInteger(1, rsaParam.Modulus), new BigInteger(1, rsaParam.Exponent));

            string pemPublicKeyStr = null;
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(sw);
                    pemWriter.WriteObject(param);
                    sw.Flush();

                    byte[] buffer = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(buffer, 0, (int)ms.Length);
                    pemPublicKeyStr = Encoding.UTF8.GetString(buffer);
                }
            }
            return pemPublicKeyStr.Replace("\r\n", "\r\n\t\t");
        }

        /// <summary>
        /// Pem公钥转成XML公钥
        /// </summary>
        /// <param name="pemPublicKeyStr"></param>
        /// <returns></returns>
        public static string PemPublicKeyToXml(string pemPublicKeyStr)
        {
            RsaKeyParameters pemPublicKey;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(pemPublicKeyStr)))
            {
                using (var sr = new StreamReader(ms))
                {
                    var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(sr);
                    pemPublicKey = (RsaKeyParameters)pemReader.ReadObject();
                }
            }

            var p = new RSAParameters
            {
                Modulus = pemPublicKey.Modulus.ToByteArrayUnsigned(),
                Exponent = pemPublicKey.Exponent.ToByteArrayUnsigned()
            };

            string xmlPublicKeyStr;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(p);
                xmlPublicKeyStr = rsa.ToXmlString(false);
            }

            return xmlPublicKeyStr;
        }

        /// <summary>
        /// XML私钥转成PEM私钥
        /// </summary>
        /// <param name="xmlPrivateKey"></param>
        /// <returns></returns>
        public static string XmlPrivateKeyToPem(string xmlPrivateKey)
        {
            RSAParameters rsaParam;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(xmlPrivateKey);
                rsaParam = rsa.ExportParameters(true);
            }

            var param = new RsaPrivateCrtKeyParameters(
                new BigInteger(1, rsaParam.Modulus), new BigInteger(1, rsaParam.Exponent), new BigInteger(1, rsaParam.D),
                new BigInteger(1, rsaParam.P), new BigInteger(1, rsaParam.Q), new BigInteger(1, rsaParam.DP), new BigInteger(1, rsaParam.DQ),
                new BigInteger(1, rsaParam.InverseQ));

            string pemPrivateKeyStr = null;
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(sw);
                    pemWriter.WriteObject(param);
                    sw.Flush();

                    byte[] buffer = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(buffer, 0, (int)ms.Length);
                    pemPrivateKeyStr = Encoding.UTF8.GetString(buffer);
                }
            }

            return pemPrivateKeyStr.Replace("\r\n", "\r\n\t\t");
        }

        /// <summary>
        /// Pem私钥转成XML私钥
        /// </summary>
        /// <param name="pemPrivateKeyStr"></param>
        /// <returns></returns>
        public static string PemPrivateKeyToXml(string pemPrivateKeyStr)
        {
            RsaPrivateCrtKeyParameters pemPrivateKey;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(pemPrivateKeyStr)))
            {
                using (var sr = new StreamReader(ms))
                {
                    var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(sr);
                    var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                    pemPrivateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
                }
            }

            var p = new RSAParameters
            {
                Modulus = pemPrivateKey.Modulus.ToByteArrayUnsigned(),
                Exponent = pemPrivateKey.PublicExponent.ToByteArrayUnsigned(),
                D = pemPrivateKey.Exponent.ToByteArrayUnsigned(),
                P = pemPrivateKey.P.ToByteArrayUnsigned(),
                Q = pemPrivateKey.Q.ToByteArrayUnsigned(),
                DP = pemPrivateKey.DP.ToByteArrayUnsigned(),
                DQ = pemPrivateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = pemPrivateKey.QInv.ToByteArrayUnsigned(),
            };

            string xmlPrivateKeyStr;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(p);
                xmlPrivateKeyStr = rsa.ToXmlString(true);
            }

            return xmlPrivateKeyStr;
        }
        #endregion

    }
}
