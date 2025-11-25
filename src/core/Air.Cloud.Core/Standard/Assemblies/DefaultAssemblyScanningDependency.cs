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
using Air.Cloud.Core.Standard.Assemblies.Model;

using System.Runtime.Loader;

namespace Air.Cloud.Core.Standard.Assemblies
{
    /// <summary>
    /// <para>zh-cn:默认的类库扫描依赖实现</para>
    /// <para>en-us:Default assembly scanning dependency implementation</para>
    /// </summary>
    public class DefaultAssemblyScanningDependency : IAssemblyScanningStandard
    {

        /// <inheritdoc/>
        public void Add(AssemblyScanningEvent Event)
        {
            IAssemblyScanningStandard.Evensts.Add(Event);
        }
        /// <inheritdoc/>
        public void Execute()
        {
            try
            {
                #region  类库扫描
                foreach (var s in AppCore.Assemblies)
                {
                    var data = AssemblyLoadContext.Default.LoadFromAssemblyName(s).GetTypes().Where(t => t.IsPublic).ToList();
                    foreach (var t in data)
                    {
                        var instances = t.GetInterfaces();
                        if (instances.Length > 0)
                        {
                            foreach (var item in IAssemblyScanningStandard.Evensts)
                            {
                                try
                                {
                                    if (instances.Count(x => x.Name == item.TargetType.Name) > 0)
                                    {
                                        item.Action.Invoke(t);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AppRealization.Output.Print(new AppPrintInformation
                                    {
                                        Title = "domain-errors",
                                        Level = AppPrintLevel.Error,
                                        Content = $"执行类库扫描时出现异常,异常信息:{ex.Message}" + t.Name,
                                        State = true
                                    });
                                }

                            }
                        }
                    }
                }
                foreach (var item in IAssemblyScanningStandard.Evensts)
                {
                    try
                    {
                        item.Finally?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new AppPrintInformation
                        {
                            Title = "domain-errors",
                            Level = AppPrintLevel.Error,
                            Content = $"执行类库扫描时出现异常,异常信息:{ex.Message}",
                            State = true
                        });
                    }
                }
                IAssemblyScanningStandard.Evensts.Clear();
                #endregion
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    Title = "domain-errors",
                    Level = AppPrintLevel.Error,
                    Content = $"执行类库扫描时出现异常,异常信息:{ex.Message}",
                    State = true
                });
            }

        }
    }
}
