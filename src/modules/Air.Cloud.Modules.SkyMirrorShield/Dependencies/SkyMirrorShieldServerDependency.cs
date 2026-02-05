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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.SkyMirror;
using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace Air.Cloud.Modules.SkyMirrorShield.Dependencies
{
    public class SkyMirrorShieldServerDependency : ISkyMirrorShieldServerStandard
    {
        public bool IsAuthorized(string Authorization, string TargetPermission)
        {

            return true;

        }

        public async Task<bool> SaveClientEndPointDataAsync(SkyMirrorShieldClientData clientData)
        {
            foreach (var item in clientData.EndpointDatas)
            {
                string Key = $"{item.Method}|{item.Path}";
                if (ISkyMirrorShieldServerStandard.ServerEndpointDatas.ContainsKey(Key))
                {
                    ISkyMirrorShieldServerStandard.ServerEndpointDatas[Key] = item;
                }
                else
                {
                    ISkyMirrorShieldServerStandard.ServerEndpointDatas.Add(Key, item);
                }
            }
            await StoreClientEndPointDataAsync();
            return true;
        }

        public async Task StoreClientEndPointDataAsync()
        {
            string CurrentStoreDatas = AppRealization.JSON.Serialize(ISkyMirrorShieldServerStandard.ServerEndpointDatas.Values);
            string FilePath = Path.Combine(AppConst.ApplicationPath, "SkyMirrorShieldStore");
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
            FilePath = $"{FilePath}/client.json";
            File.WriteAllText(FilePath, CurrentStoreDatas);
            await Task.CompletedTask;
        }

        public async Task LoadClientEndPointDataAsync()
        {
            string FilePath = Path.Combine(AppConst.ApplicationPath, "SkyMirrorShieldStore","client.json");
            if (File.Exists(FilePath))
            {
                string StoreDatas = File.ReadAllText(FilePath);
                if (string.IsNullOrEmpty(StoreDatas))
                {
                    return;
                }
                try
                {
                    ICollection<EndpointData> CurrentStoreDatas = AppRealization.JSON.Deserialize<ICollection<EndpointData>>(StoreDatas);
                    foreach (var item in CurrentStoreDatas)
                    {
                        string Key = $"{item.Method}|{item.Path}";
                        if (ISkyMirrorShieldServerStandard.ServerEndpointDatas.ContainsKey(Key))
                        {
                            ISkyMirrorShieldServerStandard.ServerEndpointDatas[Key] = item;
                        }
                        else
                        {
                            ISkyMirrorShieldServerStandard.ServerEndpointDatas.Add(Key, item);
                        }
                    }
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    string BackupFileName = $"{AppCore.Guid()}_client.json";
                    string BackUpFile = Path.Combine(AppConst.ApplicationPath, "SkyMirrorShieldStore", BackupFileName);
                    File.Move(FilePath, BackUpFile, true);
                    File.WriteAllText(FilePath, "");
                    AppRealization.Output.Print("天镜安全检查", $"读取存储的数据信息失败,异常内容:{ex.Message},已将该文件重新命名为:{BackupFileName}");
                    await Task.CompletedTask;
                }
            }
           
        }
    }
}
