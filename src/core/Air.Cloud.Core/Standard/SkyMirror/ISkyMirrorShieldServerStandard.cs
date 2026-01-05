
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

namespace Air.Cloud.Core.Standard.SkyMirror
{
    /// <summary>
    /// <para>zh-cn:身份认证标准</para>
    /// <para>en-us:Authentication standard.</para>
    /// </summary>
    public interface ISkyMirrorShieldServerStandard : IStandard
    {

        public static IDictionary<string, EndpointData> ServerEndpointDatas = new Dictionary<string,EndpointData>();

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
        /// <param name="clientData">
        ///  <para>zh-cn:客户端数据</para>
        ///  <para>en-us:Client data.</para>
        /// </param>
        /// <returns></returns>
        Task<bool> SaveClientEndPointDataAsync(SkyMirrorShieldClientData clientData);

        /// <summary>
        /// <para>zh-cn:存储客户端端点数据</para>
        /// <para>en-us:Store client endpoint data.</para>
        /// </summary>
        /// <returns></returns>
        Task StoreClientEndPointDataAsync();

        /// <summary>
        /// <para>zh-cn:加载客户端端点数据</para>
        /// <para>en-us:Load client endpoint data.</para>
        /// </summary>
        /// <returns></returns>
        Task LoadClientEndPointDataAsync(); 

    }
}
