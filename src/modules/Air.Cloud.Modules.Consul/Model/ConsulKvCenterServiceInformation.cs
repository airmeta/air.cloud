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
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Consul.Extensions;

namespace Air.Cloud.Modules.Consul.Model
{
    /// <summary>
    /// Consul Key-Value Center Service Information
    /// </summary>
    public class ConsulKvCenterServiceInformation:IKVCenterServiceOptions
    {
        /// <inheritdoc/>
        public string Key { get; set; }
        /// <inheritdoc/>
        public string Value { get; set; }
        /// <summary>
        /// Consul create index 
        /// </summary>
        public ulong CreateIndex { get; set; }
        /// <summary>
        /// Flags
        /// </summary>
        public ulong Flags { get; set; }
        /// <summary>
        /// Consul lock index
        /// </summary>
        public ulong LockIndex { get; set; }
        /// <summary>
        /// Consul modify index
        /// </summary>
        public ulong ModifyIndex { get; set; }
        /// <summary>
        /// Session
        /// </summary>
        public string Session { get; set; }
    }
}
