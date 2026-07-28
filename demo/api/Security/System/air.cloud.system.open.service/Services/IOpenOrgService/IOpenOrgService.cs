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
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Check;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Create;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Delete;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Update;

using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.open.service.Services.IOpenOrgService
{
    /// <summary>
    /// <para>zh-cn:开放组织服务接口</para>
    /// <para>en-us:Open Organization Service Interface</para>
    /// </summary>
    public interface IOpenOrgService : IDynamicService
    {

        /// <summary>
        /// <para>zh-cn:创建开放组织</para>
        /// <para>en-us:Create Open Organization</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放组织创建数据传输对象</para>
        ///  <para>en-us:Open Organization Create Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织创建结果</para>
        ///  <para>en-us:Open Organization Create Result</para>
        /// </returns>
        public Task<OpenOrgCreateResult> OpenOrgCreateAsync(OpenOrgCreateDto dto);

        /// <summary>
        /// <para>zh-cn:更新开放组织</para>
        /// <para>en-us:Update Open Organization</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放组织更新数据传输对象</para>
        ///  <para>en-us:Open Organization Update Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织更新结果</para>
        ///  <para>en-us:Open Organization Update Result</para>
        /// </returns>
        public Task<OpenOrgUpdateResult> OpenOrgUpdateAsync(OpenOrgUpdateDto dto);

        /// <summary>
        /// <para>zh-cn:检查开放组织是否存在</para>
        /// <para>en-us:Check if Open Organization Exists</para>
        /// </summary>
        /// <param name="OrgId">
        ///   <para>zh-cn:应用用户ID</para>
        ///   <para>en-us:Application User ID</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织检查结果</para>
        ///  <para>en-us:Open Organization Check Result</para>
        /// </returns>
        public Task<OpenOrgCheckResult> OpenOrgCheckAsync(string AppUserId);

        /// <summary>
        /// <para>zh-cn:删除开放组织</para>
        /// <para>en-us:Delete Open Organization</para>
        /// </summary>
        /// <param name="OrgId">
        ///   <para>zh-cn:应用用户ID</para>
        ///   <para>en-us:Application User ID</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织删除结果</para>
        ///  <para>en-us:Open Organization Delete Result</para>
        /// </returns>
        public Task<OpenOrgDeleteResult> OpenOrgDeleteAsync(string AppUserId);


    }
}
