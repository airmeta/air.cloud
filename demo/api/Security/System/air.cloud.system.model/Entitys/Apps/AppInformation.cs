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
using air.cloud.security.common.Base;
using air.cloud.security.common.Enums;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.RSA;
using Air.Cloud.Core.Plugins.Security.SM2;

using Org.BouncyCastle.Crypto.Signers;

namespace air.cloud.system.model.Entitys.Apps
{
    /// <summary>
    /// 应用信息
    /// </summary>
    [Table("SYS_APP")]
    public class AppInformation : AllEntityBase
    {
        /// <summary>
        /// <para>应用ID</para>
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }

        /// <summary>
        /// <para>应用名称</para>
        /// </summary>
        [Column("APP_NAME")]
        public string AppName { get; set; }

        /// <summary>
        /// <para>应用重定向地址</para>
        /// </summary>
        [Column("REDIRECT_URL")]
        public string? AppRedirectUrl { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [Column("LOGO", TypeName = "TEXT")]
        public string? Logo { get; set; }

        /// <summary>
        /// 是否默认应用
        /// </summary>
        [Column("IS_DEFAULT")]
        public IsOrNotEnum IsDefault { get; set; } = IsOrNotEnum.否;

        /// <summary>
        /// 应用的加密类型
        /// </summary>
        [Column("APP_ENCRYPT_TYPE")]
        public AppEntryptTypeEnum AppEncryptType { get; set; }

        /// <summary>
        /// 我方的公钥(传给应用,应用做加密)
        /// </summary>
        [Column("PUBLIC_KEY", TypeName = "TEXT")]
        public string PublicKey { get; set; }

        /// <summary>
        /// 我方的私钥(用于对方解密应用传输出去的加密数据)
        /// </summary>
        [Column("PRIVATE_KEY", TypeName = "TEXT")]
        public string PrivateKey { get; set; }

        /// <summary>
        /// 对方私钥(解密对方的数据)
        /// </summary>
        [Column("APP_PRIVATE_KEY", TypeName = "TEXT")]
        public string? AppPrivateKey { get; set; }

        /// <summary>
        /// 是否开启多因素认证
        /// </summary>
        [Column("ENABLE_MFA")]
        public IsOrNotEnum EnableMFA { get; set; } = IsOrNotEnum.否;


        /// <summary>
        /// 描述
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        /// <summary>
        /// 是否为公共应用
        /// </summary>
        [Column("IS_COMMON_APP")]
        public IsOrNotEnum IsCommonApp { get; set; } = IsOrNotEnum.否;


        /// <summary>
        /// <para>zh-cn:是否允许删除,通过应用初始化进来的数据不允许删除</para>
        /// <para>en-us:Whether deletion is allowed, data initialized through the application is not allowed to be deleted</para>
        /// </summary>
        [Column("CAN_DELETE")]
        public IsOrNotEnum CanDelete {  get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用</para> 
        /// <para>en-us:Is Enabled</para>
        /// </summary>
        [Column("IS_ENABLE")]
        public IsOrNotEnum IsEnable { get; set; }

    }

    public static class AppInfoExtensions
    {

        /// <summary>
        /// 解密默认应用的请求数据
        /// </summary>
        /// <param name="appInformation"></param>
        /// <param name="EncryptContent"></param>
        /// <returns></returns>
        public static string Decrypt(this AppInformation appInformation,string EncryptContent)
        {
            switch (appInformation.AppEncryptType)
            {
                case AppEntryptTypeEnum.RSA:
                    AppRealization.Output.Print("数据解密", $"待解密数据:{EncryptContent} 公钥: {appInformation.PublicKey} 私钥: {appInformation.AppPrivateKey} ");
                    var s =RsaEncryption.Decrypt(EncryptContent, appInformation.PublicKey,appInformation.AppPrivateKey);
                    return s;
                case AppEntryptTypeEnum.SM2:
                    return SM2Encryption.Decrypt(EncryptContent,  appInformation.AppPrivateKey);
                default:
                    return string.Empty;
            }
        }
        /// <summary>
        /// 使用我方的公钥加密数据
        /// </summary>
        /// <param name="appInformation"></param>
        /// <param name="PayLoadContent"></param>
        /// <returns></returns>
        public static string Encrypt(this AppInformation appInformation,string PayLoadContent)
        {
            if(PayLoadContent.IsNullOrEmpty()) return string.Empty; 
            switch (appInformation.AppEncryptType)
            {
                case AppEntryptTypeEnum.RSA:
                    return RsaEncryption.Encrypt(PayLoadContent,appInformation.PublicKey, appInformation.PrivateKey);
                case AppEntryptTypeEnum.SM2:
                    return SM2Encryption.Encrypt(PayLoadContent, appInformation.PrivateKey);
                default:
                    return string.Empty;
            }
        }
    }
}
