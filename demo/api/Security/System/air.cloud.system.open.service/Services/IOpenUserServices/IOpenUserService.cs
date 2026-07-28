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
using air.cloud.system.open.service.Dtos.OpenUserDtos.Check;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Create;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Delete;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Update;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.open.service.Services.IOpenUserServices
{
    /// <summary>
    /// <para>zh-cn:开放用户服务接口</para>
    /// <para>en-us:Open User Service Interface</para>
    /// </summary>
    public interface IOpenUserService : IDynamicService
    {
        /// <summary>
        /// <para>zh-cn:创建开放用户</para>
        /// <para>en-us:Create Open User</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放用户创建数据传输对象</para>
        ///  <para>en-us:Open User Create Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放用户创建结果</para>
        ///  <para>en-us:Open User Create Result</para>
        /// </returns>
        public Task<OpenUserCreateResult> OpenUserCreateAsync(OpenUserCreateDto dto);

        /// <summary>
        /// <para>zh-cn:更新账户信息</para>
        /// <para>en-us:Update Account Information</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放用户更新数据传输对象</para>
        ///  <para>en-us:Open User Update Data Transfer Object</para>
        /// </param>
        /// <returns></returns>
        public Task<OpenUserUpdateResult> OpenUserUpdateAsync(OpenUserUpdateDto dto);

        /// <summary>
        /// <para>zh-cn:检查用户是否存在</para>
        /// <para>en-us:Check if User Exists</para>
        /// </summary>
        /// <param name="AppUserId">
        ///  <para>zh-cn:应用用户ID</para>
        ///  <para>en-us:Application User ID</para>
        /// </param>
        /// <returns></returns>
        public Task<OpenUserCheckResult> OpenUserCheckAsync(string AppUserId);

        /// <summary>
        /// <para>zh-cn:删除开放用户</para>
        /// <para>en-us:Delete Open User</para>
        /// </summary>
        /// <param name="AppUserId">
        ///  <para>zh-cn:应用用户ID</para>
        ///  <para>en-us:Application User ID</para>
        /// </param>
        /// <returns></returns>
        public Task<OpenUserDeleteResult> OpenUserDeleteAsync(string AppUserId);


    }
}
