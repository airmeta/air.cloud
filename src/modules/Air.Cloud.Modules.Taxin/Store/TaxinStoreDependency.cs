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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.Store;
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Events;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Core.Standard.Taxin.Tools;

namespace Air.Cloud.Modules.Taxin.Store
{
    /// <summary>
    /// <para>zh-cn:Taxin 存储实现</para>
    /// <para>en-us:Taxin store dependency</para>
    /// </summary>
    public class TaxinStoreDependency : ITaxinStoreStandard
    {     
        /// <summary>
           /// <para>zh-cn:Taxin 配置选项</para>
           /// <para>en-us:Taxin options </para>
           /// </summary>
        private TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();
        /// <summary>
        /// <para>zh-cn:无参构造函数</para>
        /// <para>en-us:No parameter constructor</para>
        /// </summary>
        public TaxinStoreDependency()
        {
            SetPersistenceHandler += new EventHandler<TaxinStoreEventArgs>(DefaultSetPersistenceHandler);
            GetPersistenceHandler += new EventHandler<TaxinStoreEventArgs>(DefaultGetPersistenceHandler);
        }
        #region 事件
        /// <inheritdoc/>
        public event EventHandler<TaxinStoreEventArgs> SetPersistenceHandler = null;
        /// <inheritdoc/>
        public event EventHandler<TaxinStoreEventArgs> GetPersistenceHandler = null;


        /// <summary>
        /// <para>zh-cn:默认Taxin存储Set事件</para>
        /// <para>en-us:By default, Taxin stores the Set event</para>
        /// </summary>
        /// <param name="sender">
        /// <para>zh-cn:事件触发人</para>
        /// <para>en-us:Event sender</para>
        /// </param>
        /// <param name="classInfoEvent">
        /// <para>zh-cn:事件参数</para>
        /// <para>en-us:Event args</para>
        /// </param>
        public void DefaultSetPersistenceHandler(object sender, TaxinStoreEventArgs classInfoEvent)
        {
            //默认事件处理程序
            if (AppEnvironment.IsDevelopment || Options.PersistenceOutput)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    Title = "Set Taxin data storage",
                    Content = AppRealization.JSON.Serialize(classInfoEvent.Packages)
                });
            }
        }
        /// <summary>
        /// <para>zh-cn:默认Taxin存储Get事件</para>
        /// <para>en-us:By default, Taxin stores the Get event</para>
        /// </summary>
        /// <param name="sender">
        /// <para>zh-cn:事件触发人</para>
        /// <para>en-us:Event sender</para>
        /// </param>
        /// <param name="classInfoEvent">
        /// <para>zh-cn:事件参数</para>
        /// <para>en-us:Event args</para>
        /// </param>
        public void DefaultGetPersistenceHandler(object sender, TaxinStoreEventArgs classInfoEvent)
        {
            //默认事件处理程序
            if (AppEnvironment.IsDevelopment || Options.PersistenceOutput)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    Title = "Get Taxin data storage",
                    Content = AppRealization.JSON.Serialize(classInfoEvent.Packages)
                });
            }
        }
        #endregion
        #region 实现
        /// <inheritdoc/>
        public async Task<IDictionary<string, IEnumerable<TaxinRouteDataPackage>>> GetPersistenceAsync()
        {
            IDictionary<string, IEnumerable<TaxinRouteDataPackage>> data = new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>();
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                    data=TaxinStoreTools.FolderGet();
                    break;
                case PersistenceMethodEnum.Cache:
                    if (AppRealization.Cache == null)
                    {
                        AppRealization.Output.Error(new Exception("无法获取到缓存模块信息"));
                    }
                    var data1 = AppRealization.Cache.GetCache(ITaxinStoreStandard.GetPersistenceCachePath(Options.PersistencePath, ":"));
                    if (data1.IsNullOrEmpty())
                    {
                        AppRealization.Output.Print(new AppPrintInformation()
                        {
                            Level = AppPrintLevel.Information,
                            State = true,
                            Content = "The StoreData Is Empty",
                            Title = "Taxin Notice"
                        });
                    }
                    else
                    {
                        data = AppRealization.JSON.Deserialize<Dictionary<string, IEnumerable<TaxinRouteDataPackage>>>(data1);
                    }
                    break;
                case PersistenceMethodEnum.KVCenter:
                    if (AppRealization.KVCenter == null)
                    {
                        AppRealization.Output.Error(new Exception("无法获取到键值对模块信息"));
                    }
                    DefaultKVCenterServiceOptions MetaStoreContent = await AppRealization.KVCenter.GetAsync<DefaultKVCenterServiceOptions>(ITaxinStoreStandard.GetPersistenceKVStoreMetaPath(Options.PersistencePath));
                    if (!MetaStoreContent.Value.IsNullOrEmpty())
                    {

                        List<string> Keys = AppRealization.JSON.Deserialize<List<string>>(MetaStoreContent.Value);
                        foreach (var item in Keys)
                        {
                            DefaultKVCenterServiceOptions Values = await AppRealization.KVCenter.GetAsync<DefaultKVCenterServiceOptions>(ITaxinStoreStandard.GetPersistenceKVStorePath(Options.PersistencePath, MD5Encryption.GetMd5By32(item)));

                            data.Add(item, AppRealization.JSON.Deserialize<IEnumerable<TaxinRouteDataPackage>>(Values.Value));
                        }
                    }
                    break;
            }
            ITaxinStoreStandard.Packages = data;
            GetPersistenceHandler(this, new TaxinStoreEventArgs()
            {
                Packages = ITaxinStoreStandard.Packages
            });
            return data;
        }
        /// <inheritdoc/>
        public async Task SetPersistenceAsync(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages)
        {
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                    TaxinStoreTools.FolderSet(Packages);
                    break;
                case PersistenceMethodEnum.Cache:
                    if (AppRealization.Cache == null)
                    {
                        AppRealization.Output.Error(new Exception("无法获取到缓存模块信息"));
                    }
                    AppRealization.Cache.SetCache(ITaxinStoreStandard.GetPersistenceCachePath(Options.PersistencePath, ":"), AppRealization.JSON.Serialize(Packages));
                    break;
                case PersistenceMethodEnum.KVCenter:
                    if (AppRealization.KVCenter == null)
                    {
                        AppRealization.Output.Error(new Exception("无法获取到键值对模块信息"));
                    }
                    #region  重新设置路由信息

                    IList<string> AllKeys= Packages.Keys.ToList();

                    await AppRealization.KVCenter.AddOrUpdateAsync(ITaxinStoreStandard.GetPersistenceKVStoreMetaPath(Options.PersistencePath), AppRealization.JSON.Serialize(AllKeys));

                    foreach (var item in AllKeys)
                    {
                        string Value = AppRealization.JSON.Serialize(Packages[item]);
                        await AppRealization.KVCenter.AddOrUpdateAsync(ITaxinStoreStandard.GetPersistenceKVStorePath(Options.PersistencePath, MD5Encryption.GetMd5By32(item)), Value);
                    }
                    #endregion
                    break;
            }
            ITaxinStoreStandard.Packages = Packages;
            GetPersistenceHandler(this, new TaxinStoreEventArgs()
            {
                Packages = ITaxinStoreStandard.Packages
            });
            await Task.CompletedTask;
        }
        #endregion
    }
}
