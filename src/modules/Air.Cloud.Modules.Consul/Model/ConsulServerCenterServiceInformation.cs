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
namespace Air.Cloud.Modules.Consul.Model
{
    public class ConsulServerCenterServiceInformation 
    {
        public string ServiceAddress { get; set; }
        public string ServiceName { get; set; }
        public string ServiceKey { get; set; }
        public string[] ServiceValues { get; set; }

        public IList<object> ServerDetails { get; set; }
    }

    public class ServerDetailInformation
    {
        public string Node { get; set; }

        public string Address { get; set; }

        public string ServiceID { get; set; }

        public string ServiceName { get; set; }

        public string ServiceAddress { get; set; }

        public string[] ServiceTags { get; set; }

        public int ServicePort { get; set; }

        public IList<KeyValuePair<string, string>> ServiceTaggedAddresses { get; set; }

        public bool ServiceEnableTagOverride { get; set; }

        public IDictionary<string, string> ServiceMeta { get; set; }
    }
}
