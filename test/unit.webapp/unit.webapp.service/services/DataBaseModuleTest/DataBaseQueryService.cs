
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

using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Domains;

namespace unit.webapp.service.services.DataBaseModuleTest
{
    [Route("api/db")]
    public class DataBaseQueryService : IDataBaseQueryService
    {
        private readonly ITestDomain? Domain;
        private readonly IServerCenterStandard serverCenterStandard;
        private readonly IKVCenterStandard kVCenterStandard;

        public DataBaseQueryService(IServerCenterStandard serverCenterStandard, IKVCenterStandard kVCenterStandard, ITestDomain? domain = null)
        {
            Domain = domain;
            this.serverCenterStandard = serverCenterStandard;
            this.kVCenterStandard = kVCenterStandard;
        }

        [HttpGet("query"),AllowAnonymous]
        public object Query()
        {
            return Domain?.Search("a09cdb089b7f48498090d1f7f11c0e7b") ?? Enumerable.Empty<object>();
        }
        [HttpGet("server"), AllowAnonymous]
        public async Task<object> Sq()
        {
            var Result = (await serverCenterStandard.QueryAsync<ConsulServerCenterServiceOptions>()).OrderBy(s => s.ServiceName).ToList();
            return Result;
        }
        [HttpGet("kvs"), AllowAnonymous]
        public async Task<object> Sq1()
        {
            var Result = (await kVCenterStandard.QueryAsync<ConsulKvCenterServiceInformation>()).OrderBy(s => s.Value).ToList();
            return Result;
        }

        [HttpGet("batch"), AllowAnonymous]
        public async Task<bool> Batch()
        {
            if (Domain == null)
            {
                return false;
            }
            return await Domain.BatchInsertAsync();
        }




    }
}
