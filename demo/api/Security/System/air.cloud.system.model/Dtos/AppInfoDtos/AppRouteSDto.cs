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
using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace air.cloud.system.model.Dtos.AppInfoDtos
{
    public  class AppRouteSDto
    {
        /// <summary>
        /// <para>zh-cn:编号</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:路由地址</para>
        /// <para>en-us:Route Address</para>
        /// </summary>
        public string Route { get; set; }
        /// <summary>
        /// <para>zh-cn:应用ID</para>
        /// <para>en-us:App ID</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:是否允许匿名访问</para>
        /// <para>en-us:Whether to allow anonymous access</para>
        /// </summary>
        public IsOrNotEnum? AllowAnonymous { get; set; }


        /// <summary>
        /// <para>zh-cn:是否需要授权访问</para>
        /// <para>en-us:Whether authorization is required for access</para>
        /// </summary>
        public IsOrNotEnum? RequiresAuthorization { get; set; }


        /// <summary>
        /// <para>zh-cn:请求方法</para>
        /// <para>en-us:Request Method</para>
        /// </summary>
        public string Method { get; set; }  


        /// <summary>
        /// <para>zh-cn:授权元信息</para>
        /// <para>en-us:Authorization Meta Information</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn:存储授权信息,JSON数组字符串,元素类型:<see cref="EndPointAuthorizeData"/></para>
        ///   <para>en-us:Store authorization information, JSON array string, element type: <see cref="EndPointAuthorizeData"/></para>
        /// </remarks>
        public IList<EndPointAuthorizeData> AuthorizationMetas { get; set; }

        /// <summary>
        /// <para>zh-cn:授权元信息-序列化字符串</para> 
        /// <para>en-us:Authorization Meta Information - Serialized String</para>
        /// </summary>
        public string AuthorizationMeta=>AppRealization.JSON.Serialize(AuthorizationMetas);



        /// <summary>
        /// <para>zh-cn:创建时间</para>
        /// <para>en-us:Create Time</para>  
        /// </summary>
        public DateTime CreateTime { get; set; }

        public void SetAuthorizationMetasFromString(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                AuthorizationMetas = AppRealization.JSON.Deserialize<IList<EndPointAuthorizeData>>(jsonString);
            }
            else
            {
                AuthorizationMetas = new List<EndPointAuthorizeData>();
            }
        }

    }
}
