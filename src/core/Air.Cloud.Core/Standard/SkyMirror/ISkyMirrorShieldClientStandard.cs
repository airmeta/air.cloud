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
using Air.Cloud.Core.Standard.SkyMirror.Model;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Core.Standard.SkyMirror
{
    /// <summary>
    /// 身份认证标准
    /// </summary>
    public interface ISkyMirrorShieldClientStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:客户端端点数据列表</para>
        /// <para>en-us:Client endpoint data list</para>
        /// </summary>

        public static IList<EndpointData> ClientEndpointDatas=new List<EndpointData>();   


        /// <summary>
        /// <para>zh-cn:获取访问令牌</para>
        /// <para>en-us:Get access token</para>
        /// </summary>
        /// <param name="httpContext">
        ///  <para>zh-cn:Http上下文</para>
        ///  <para>en-us:Http context</para>
        /// </param>
        /// <param name="Claims">
        ///  <para>zh-cn:用户声明信息</para>
        ///  <para>en-us:User claims information</para>
        /// </param>
        /// <param name="Permissions">
        ///  <para>zh-cn:用户权限信息</para>
        ///  <para>en-us:User permissions information</para>
        /// </param>
        /// <returns></returns>
        string GetAccessToken(HttpContext httpContext,IDictionary<string,string> Claims,IEnumerable<string> Permissions);

        /// <summary>
        /// <para>zh-cn:获取访问令牌</para>
        /// <para>en-us:Get access token</para>
        /// </summary>
        /// <param name="httpContext">
        ///  <para>zh-cn:Http上下文</para>
        ///  <para>en-us:Http context</para>
        /// </param>
        /// <param name="Claims">
        ///  <para>zh-cn:用户声明信息</para>
        ///  <para>en-us:User claims information</para>
        /// </param>
        /// <param name="Permissions">
        ///  <para>zh-cn:用户权限信息</para>
        ///  <para>en-us:User permissions information</para>
        /// </param>
        /// <returns></returns>
        string GetRefreshToken(HttpContext httpContext, IDictionary<string, string> Claims, IEnumerable<string> Permissions);

        /// <summary>
        /// <para>zh-cn:尝试加载客户端端点数据</para>
        /// <para>en-us:Try to load client endpoint data</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:端点数据列表</para>
        ///  <para>en-us:Endpoint data list</para>
        /// </returns>
        IList<EndpointData> TryLoadClientEndpointData();


        /// <summary>
        /// <para>zh-cn:尝试推送客户端端点数据</para>
        /// <para>en-us:Try to push client endpoint data</para>
        /// </summary>
        /// <returns></returns>
        Task<bool> TryPushClientEndpointData();

    }
}
