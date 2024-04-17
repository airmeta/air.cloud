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
using Air.Cloud.Core.Standard.Print;

using System.Runtime.Loader;

namespace Air.Cloud.Core.Standard.Assemblies
{
    /// <summary>
    /// 默认的类库扫描实现
    /// </summary>
    public class DefaultAssemblyScanningDependency : IAssemblyScanningStandard
    {
        public void Add(KeyValuePair<string, Action<Type>> keyValuePair)
        {
            IAssemblyScanningStandard.Evensts.TryAdd(keyValuePair.Key,keyValuePair.Value);
        }
        public void Scanning()
        {
            try
            {
                #region  类库扫描
                foreach (var s in AppCore.Assemblies)
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyName(s).GetTypes().Where(t => t.IsPublic).ToList().ForEach(t =>
                    {
                        var instances = t.GetInterfaces();
                        if (instances.Length > 0)
                        {
                            foreach (var item in IAssemblyScanningStandard.Evensts)
                            {
                                try
                                {
                                    if(instances.Count(x=>x.Name== item.Key) > 0)
                                    {
                                        item.Value.Invoke(t);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AppRealization.Output.Print(new AppPrintInformation
                                    {
                                        Title = "domain-errors",
                                        Level = AppPrintInformation.AppPrintLevel.Error,
                                        Content = $"执行类库扫描时出现异常,异常信息:{ex.Message}"+t.Name,
                                        State = true
                                    });
                                }
                                
                            }
                        }
                    });
                }
                #endregion
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    Title = "domain-errors",
                    Level = AppPrintInformation.AppPrintLevel.Error,
                    Content = $"执行类库扫描时出现异常,异常信息:{ex.Message}",
                    State = true
                });
            }

        }
    }
}
