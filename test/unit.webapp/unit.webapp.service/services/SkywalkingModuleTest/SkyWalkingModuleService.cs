
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

using Air.Cloud.Core;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.JinYiWei;
using Air.Cloud.Core.Standard.Taxin.Client;

using Microsoft.AspNetCore.Mvc;

using Serilog.Context;

using SkyApm.Tracing;
using SkyApm.Tracing.Segments;

using unit.webapp.model.Dto;

namespace unit.webapp.service.services.SkywalkingModuleTest
{
    [Route("api")]
    public  class SkyWalkingModuleService : ISkyWalkingModuleService,IDynamicService
    {
        private readonly ITraceLogStandard traceLog;
        private readonly ITaxinClientStandard taxinClientStandard;
        public SkyWalkingModuleService(ITraceLogStandard Log,ITaxinClientStandard taxinClientStandard)
        {
            this.traceLog = Log;
            this.taxinClientStandard=taxinClientStandard;
        }
        /// <summary>
        /// 测试skywalking
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// 测试该接口需要开启skywalking 需要开启taxinserver
        /// </remarks>
        /// <returns></returns>
        [HttpPost("skywalking/test")]
        public async Task<object> Test(TestSDto dto)
        {
            traceLog.Write(dto);
            var data = await taxinClientStandard.SendAsync<object>("taxin.service.test");
            return new
            {
                dto,
                data
            };
        }
    }
}
