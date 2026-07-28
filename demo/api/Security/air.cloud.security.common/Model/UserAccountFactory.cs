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
using air.cloud.security.common.Enums;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Security.RSA;
using Air.Cloud.Core.Plugins.Security.SM2;

using Newtonsoft.Json;

using System.Text.Json.Serialization;

namespace air.cloud.security.common.Model
{
    /// <summary>
    /// 用户账户信息
    /// </summary>
    public class UserAccountFactory
    {
        /// <summary>
        /// <para>zh-cn:登录应用标识</para>
        /// <para>en-us:Login Application ID</para>
        /// </summary>
        public string LoginAppId { get; set; }

        /// <summary>
        /// <para>zh-cn:登录来源</para>
        /// <para>en-us:Login Source</para>
        /// </summary>
        public LoginSourceEnum LoginSource { get; set; }

        /// <summary>
        /// <para>zh-cn:用户账户</para>
        /// <para>en-us:User Account</para> 
        /// </summary>
        public string Account { get; set; }


        /// <summary>
        ///  <para>zh-cn:用户标识</para>
        ///  <para>en-us:User ID</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:应用用户标识</para>
        /// <para>en-us:Application User ID</para>
        /// </summary>
        public string AppUserId { get; set; }

        /// <summary>
        /// <para>zh-cn:用户名称</para>
        /// <para>en-us:User Name</para>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <para>zh-cn:身份证号码</para>
        /// <para>en-us:ID Card Number</para>
        /// </summary>
        public string IdCardNo { get; set; }

        /// <summary>
        /// <para>zh-cn:手机号码</para>
        /// <para>en-us:Phone Number</para>
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// <para>zh-cn:邮箱账户</para>
        /// <para>en-us:Email Account</para>
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// <para>zh-cn:登录后颁发的票据信息</para>
        /// <para>en-us:Ticket Information Issued After Login</para>
        /// </summary>
        public string Ticket { get; set; }

        /// <summary>
        /// <para>zh-cn:票据过期时间</para>
        /// <para>en-us:Ticket Expiration Time</para>
        /// </summary>
        public DateTime ExpiredAt { get; set; }

        /// <summary>
        /// <para>zh-cn:加密类型</para>
        /// <para>en-us:Encryption Type</para>
        /// </summary>
        public AppEntryptTypeEnum EntryptType { get; set; }

        /// <summary>
        /// <para>zh-cn:私钥</para>
        /// <para>en-us:Private Key</para>
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// <para>zh-cn:用户职位信息</para>
        /// <para>en-us:User Assignment Information</para>
        /// </summary>
        public IList<AssignmentsInfo> Assignments { get; set; }=new List<AssignmentsInfo>();

        /// <summary>
        /// <para>zh-cn:用户所属部门列表</para>
        /// <para>en-us:List of departments the user belongs to</para>
        /// </summary>
        public IList<DepartmentsInfo> Departments { get; set; }=new List<DepartmentsInfo>();

        /// <summary>
        /// <para>zh-cn:身份证号码 解密版</para>
        /// <para>en-us:ID Card Number Decrypted Version</para>
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string IdCardNoDecrypt => IdCardNo.IsNullOrEmpty()?string.Empty: (EntryptType == AppEntryptTypeEnum.RSA ? RsaEncryption.Decrypt(IdCardNo, PrivateKey, PrivateKey) : SM2Encryption.Decrypt(IdCardNo, PrivateKey));

        /// <summary>
        /// <para>zh-cn:手机号码 解密版</para>
        /// <para>en-us:Phone Number Decrypted Version</para>
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string PhoneNumberDecrypt => PhoneNumber.IsNullOrEmpty() ? string.Empty : (EntryptType == AppEntryptTypeEnum.RSA ? RsaEncryption.Decrypt(PhoneNumber, PrivateKey, PrivateKey) : SM2Encryption.Decrypt(PhoneNumber, PrivateKey));



        /// <summary>
        /// <para>zh-cn:获取用户账户的载荷信息</para>
        /// <para>en-us:Get the payload information of the user account</para>
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetAccountPublicPayLoad()
        {
            #region 加载用户关联信息
            IDictionary<string, string> payLoad = new Dictionary<string, string>()
            {
                    { "UserName",this.UserName??string.Empty },
                    { "AppUserId",this.AppUserId??string.Empty },
                    { "Account",this.Account??string.Empty },
                    { "PhoneNumber",this.PhoneNumber??string.Empty },
                    { "Email",this.Email??string.Empty },
                    { "Departments",AppRealization.JSON.Serialize(this.Departments)},
                    { "Assignments",AppRealization.JSON.Serialize(this.Assignments)}
            };
            #endregion
            return payLoad;
        }

        /// <summary>
        /// <para>zh-cn:用户所属部门列表</para>
        /// <para>en-us:List of departments the user belongs to</para>
        /// </summary>
        public class DepartmentsInfo
        {
            /// <summary>
            /// 部门ID
            /// </summary>
            public string DepartmentId { get; set; }
            /// <summary>
            /// 部门名称
            /// </summary>
            public string DepartmentName { get; set; }

        }

        /// <summary>
        /// <para>zh-cn:职位信息</para>
        /// <para>en-us:Assignment Information</para>
        /// </summary>
        public class AssignmentsInfo
        {
            /// <summary>
            /// 职位ID
            /// </summary>
            public string AssignmentId { get; set; }
            /// <summary>
            /// 职位名称
            /// </summary>
            public string AssignmentName { get; set; }
            /// <summary>
            /// <para>zh-cn:所属部门ID</para>   
            /// <para>en-us:Department ID</para>
            /// </summary>
            public string DepartmentId { get; set; }
        }
    }

}
