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
using Air.Cloud.Core.Standard.Security.Model;

namespace Air.Cloud.Core.Standard.Security
{
    /// <summary>
    /// <para>zh-cn:身份认证标准</para>
    /// <para>en-us:Authentication standard.</para>
    /// </summary>
    public interface ISecurityServerStandard : IStandard
    {

        public static IList<EndpointData> ServerEndpointDatas = new List<EndpointData>();

        /// <summary>
        /// <para>zh-cn:检查当前用户是否具有给定权限的授权</para>
        /// <para>en-us:Check if the current user is authorized for the given permission.</para>
        /// </summary>
        /// <param name="Authorization">
        ///  <para>zh-cn:身份验证令牌</para>
        ///  <para>en-us:Authentication token.</para>
        /// </param>
        /// <param name="TargetPermission">
        ///  <para>zh-cn:目标权限标识</para>
        ///  <para>en-us:Target permission identifier.</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:如果用户具有该权限的授权则返回 true，否则返回 false。</para>
        ///  <para>en-us:Returns true if the user is authorized for the permission, otherwise false.</para>
        /// </returns>
        bool IsAuthorized(string Authorization,string TargetPermission);


        /// <summary>
        /// <para>zh-cn:保存客户端端点数据</para>
        /// <para>en-us:Save client endpoint data.</para>
        /// </summary>
        /// <param name="endpointDatas">
        ///  <para>zh-cn:端点数据列表</para>
        ///  <para>en-us:List of endpoint data.</para>
        /// </param>
        /// <returns></returns>
        bool SaveClientEndPointData(IList<EndpointData> endpointDatas);
    }
}
