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
using Air.Cloud.Modules.Consul.Extensions;

namespace Air.Cloud.Modules.Consul.Model
{
    public class ConsulConfigCenterServiceInformation 
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ulong CreateIndex { get; set; }

        public ulong Flags { get; set; }
        public ulong LockIndex { get; set; }
        public ulong ModifyIndex { get; set; }
        public string Session { get; set; }
    }
}
