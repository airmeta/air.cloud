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
using Air.Cloud.Core.Plugins.Security.RSA;

using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Core.Plugins.Cert
{
    /// <summary>
    /// 证书加载器
    /// </summary>
    public class SystemRSACertLoader
    {
        //帮我完善这个类 允许使用类里面的方法加载指定路径的证书文件 
        //证书文件的路径可以从配置文件里面读取
        //证书文件的密码可以从配置文件里面读取

        /// <summary>
        /// 证书公钥与私钥信息
        /// </summary>
        public Tuple<string, string> Certificate { get; set; }

        /// <summary>
        /// 证书名称
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// 加载RSA证书内容信息
        /// </summary>
        /// <returns></returns>
        public Tuple<string, string> LoadRsaCert()
        {
            //读取配置文件里面的证书文件路径
            var Path = AppConfiguration.Configuration["AppSecurity:RSACertInfo:RSACertPath"];

            //读取配置文件里面的证书文件密码
            var Password = AppConfiguration.Configuration["AppSecurity:RSACertInfo:RSACertPassword"];

            //return new (GetPublicKey(Path), GetPrivateKey(Path, Password));
            return new(Path, Password);
        }

        /// <summary>
        /// 加载公钥信息
        /// </summary>
        /// <param name="pubKeyFile"></param>
        /// <returns></returns>
        private static string GetPublicKey(string pubKeyFile, string password)
        {
            try
            {
                var pc = new X509Certificate2(pubKeyFile, password);
                if (pc == null) throw new Exception("试图加载错误的公钥或公钥不存在");
                string PrivateKeys = pc.PrivateKey?.ToXmlString(false);
                if (PrivateKeys == null) throw new Exception("试图加载错误的公钥或公钥不存在");
                string PublicKey = RsaTools.XmlPublicKeyToPem(PrivateKeys);
                return PublicKey;
            }
            catch (Exception ex)
            {
                throw new Exception("试图加载错误的公钥或公钥不存在", ex);
            }
        }
    }
}
