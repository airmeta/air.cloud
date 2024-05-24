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
using Air.Cloud.Core.Standard.Taxin.Events;
using Air.Cloud.Core.Standard.Taxin.Tools;
using Air.Cloud.Modules.Taxin;
using Air.Cloud.Modules.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Store
{
    /// <summary>
    /// <para>zh-cn:默认Taxin 存储实现</para>
    /// <para>en-us:Default Taxin store dependency</para>
    /// </summary>
    public class DefaultTaxinStoreDependency : ITaxinStoreStandard
    {
        /// <summary>
        /// <para>zh-cn:Taxin 配置选项</para>
        /// <para>en-us:Taxin options </para>
        /// </summary>
        public TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();
        /// <summary>
        /// <para>zh-cn:无参构造函数</para>
        /// <para>en-us:No parameter constructor</para>
        /// </summary>
        public DefaultTaxinStoreDependency()
        {
            SetPersistenceHandler += new EventHandler<TaxinStoreEventArgs>(DefaultSetPersistenceHandler);
            GetPersistenceHandler += new EventHandler<TaxinStoreEventArgs>(DefaultGetPersistenceHandler);
        }
        #region 事件
        /// <inheritdoc/>
        public event EventHandler<TaxinStoreEventArgs> SetPersistenceHandler=null;
        /// <inheritdoc/>
        public  event EventHandler<TaxinStoreEventArgs> GetPersistenceHandler = null;
       

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
            if (AppEnvironment.IsDevelopment|| Options.PersistenceOutput)
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
            Dictionary<string, IEnumerable<TaxinRouteDataPackage>> data = new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>();
            switch (Options.PersistenceMethod)
            {
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.Folder:
                    data=(Dictionary<string, IEnumerable<TaxinRouteDataPackage>>)TaxinStoreTools.FolderGet();
                    break;
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.Cache:
                    data=AppRealization.Cache.GetCache<Dictionary<string, IEnumerable<TaxinRouteDataPackage>>>(Options.PersistenceKey);
                    break;
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.KVCenter:
                    data = await AppRealization.KVCenter.GetAsync<Dictionary<string, IEnumerable<TaxinRouteDataPackage>>>(Options.PersistenceKey);
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
        public async  Task SetPersistenceAsync(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages)
        {
            switch (Options.PersistenceMethod)
            {
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.Folder:
                    TaxinStoreTools.FolderSet(Packages);
                    break;
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.Cache:
                    AppRealization.Cache.SetCache(Options.PersistenceKey,AppRealization.JSON.Serialize(Packages));
                    break;
                case Cloud.Modules.Taxin.Server.PersistenceMethodEnum.KVCenter:
                    await AppRealization.KVCenter.AddOrUpdateAsync(Options.PersistenceKey, AppRealization.JSON.Serialize(Packages));
                    break;
            }
            ITaxinStoreStandard.Packages = Packages;
            SetPersistenceHandler(this, new TaxinStoreEventArgs()
            {
                Packages = ITaxinStoreStandard.Packages
            });
            await Task.CompletedTask;
        }
        #endregion
    }

}
