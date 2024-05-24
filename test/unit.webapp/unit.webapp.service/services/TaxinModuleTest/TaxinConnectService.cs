
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

using Air.Cloud.Core.Standard.Taxin.Attributes;
using Air.Cloud.Core.Standard.Taxin.Client;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace unit.webapp.service.services.TaxinModuleTest
{
    [Route("api/taxin")]
    public  class TaxinConnectService: ITaxinConnectService
    {
        private readonly ITaxinClientStandard Client;
        public TaxinConnectService(ITaxinClientStandard taxinClient)
        {
            this.Client = taxinClient;
        }

        [HttpGet("test1")]
        [AllowAnonymous]
        public async Task<object> ClientA()
        {
            try
            {
                var data = await Client.SendAsync<object>("taxin.service.test");
                return new { name = "132", data = data };
            }
            catch (Exception ex)
            {
                return new { name = "132", data = ex.Message };
            }

        }
        [HttpGet("test")]
        [AllowAnonymous]
        [TaxinService("taxin.service.test")]
        public object ClientB()
        {
            return new { name = "TaxinServiceTest" };
        }
    }
}
