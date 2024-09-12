
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Domains;

namespace unit.webapp.service.services.DataBaseModuleTest
{
    [Route("api/db")]
    public class DataBaseQueryService : IDataBaseQueryService
    {
        private readonly ITestDomain Domain;
        public DataBaseQueryService(ITestDomain domain)
        {
            Domain = domain;
        }
        [Route("query"),AllowAnonymous]
        public object Query()
        {
            return Domain.Search(s => s.UserId == "a09cdb089b7f48498090d1f7f11c0e7b");
        }
    }
}
