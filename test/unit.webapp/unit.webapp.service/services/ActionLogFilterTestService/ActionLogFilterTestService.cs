﻿
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

using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Dto;

namespace unit.webapp.service.services.ActionLogFilterTestService
{
    [Route("api")]
    public  class ActionLogFilterTestService : IDynamicService, ITransient, IActionLogFilterTestService
    {
        [HttpPost("logfilter/test")]
        public object Test(TestSDto dto)
        {
            //这里需要在ActionLogFilter里面打断点，看看是否能够获取到dto
            return dto;
        }
    }
}
