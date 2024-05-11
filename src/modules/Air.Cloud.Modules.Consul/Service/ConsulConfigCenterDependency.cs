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
using Air.Cloud.Modules.Consul.Model;

using Consul;

using Mapster;

using System.Text;

namespace Air.Cloud.Modules.Consul.Service
{
    public class ConsulConfigCenterDependency : IKVCenterStandard
    {
        public static ConsulClient ConsulClient => ConsulServerCenterDependency.ConsulClient;
        public async Task<IList<T>> Query<T>() where T : class, new()
        {
            var Result = await ConsulClient.KV.List(string.Empty);
            if (Result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return (IList<T>)Result.Response.Select(s => new ConsulConfigCenterServiceInformation
                {
                    Key = s.Key,
                    Value = s.Value == null ? string.Empty : Encoding.UTF8.GetString(s.Value),
                    LockIndex = s.LockIndex,
                    ModifyIndex = s.ModifyIndex,
                    CreateIndex = s.CreateIndex,
                    Flags = s.Flags,
                    Session = s.Session
                }).ToList();
            }
            else
            {
                return new List<T>();
            }
        }
        public async Task<bool> AddOrUpdate(string Key, string Value)
        {
            var Result = await ConsulClient.KV.Get(Key);
            if (Result.Response != null)
            {
                Result.Response.Value = Encoding.UTF8.GetBytes(Value);
                var EditResult = await ConsulClient.KV.CAS(Result.Response);
                return EditResult.StatusCode == System.Net.HttpStatusCode.OK;
            }
            else
            {
                var EditResult = ConsulClient.KV.Put(new KVPair(Key)
                {
                    Value = Encoding.UTF8.GetBytes(Value)
                }).Result;
                return EditResult.StatusCode == System.Net.HttpStatusCode.OK;
            }
        }
        public async Task<bool> Delete(string Key)
        {
            var queryResult = await ConsulClient.KV.Get(Key);
            var response = queryResult.Response;
            WriteResult<bool> Result = null;
            if (response != null)
            {
                Result = await ConsulClient.KV.DeleteCAS(response);
            }
            return Result != null && Result.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<T> Get<T>(string Key) where T : class, new()
        {
            var Result = await ConsulClient.KV.Get(Key);
            if (Result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var data = new ConsulConfigCenterServiceInformation()
                {
                    Key = Result.Response.Key,
                    Value = Result.Response.Value == null ? string.Empty : Encoding.UTF8.GetString(Result.Response.Value),
                    LockIndex = Result.Response.LockIndex,
                    ModifyIndex = Result.Response.ModifyIndex,
                    CreateIndex = Result.Response.CreateIndex,
                    Flags = Result.Response.Flags,
                    Session = Result.Response.Session
                };
                return data.Adapt<T>();
            }
            else
            {
                return default;
            }

        }
    }
}
