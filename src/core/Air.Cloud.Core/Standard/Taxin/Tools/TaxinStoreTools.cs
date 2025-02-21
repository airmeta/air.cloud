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
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Tools
{
    /// <summary>
    /// <para>zh-cn:Taxin 存储工具</para>
    /// <para>en-us:Taxin storage tools</para>
    /// </summary>
    public static class TaxinStoreTools
    {
        /// <summary>
        /// <para>zh-xn:获取文件夹存储中的数据</para>
        /// <para>en-us:GetAsync the data in the folder storage</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:存储的数据</para>
        /// <para>en-us:Stored data</para>
        /// </returns>
        public static IDictionary<string, IEnumerable<TaxinRouteDataPackage>> FolderGet()
        {
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            IDictionary<string, IEnumerable<TaxinRouteDataPackage>> pairs = new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>();
            string FolderPath = Path.Combine(AppConst.ApplicationPath, options.PersistencePath);
            if (Directory.Exists(FolderPath))
            {
                string[] files = Directory.GetFiles(FolderPath);
                foreach (var file in files)
                {
                    string content = File.ReadAllText(file);
                    pairs.Add(Path.GetFileNameWithoutExtension(file), AppRealization.JSON.Deserialize<IEnumerable<TaxinRouteDataPackage>>(content));
                }
            }
            return pairs;
        }
        /// <summary>
        /// <para>zh-xn:将数据存储到文件夹中</para>
        /// <para>en-us:Store the data in a folder</para>
        /// </summary>
        /// <param name="pairs">
        /// <para>zh-cn:存储的数据</para>
        /// <para>en-us:Stored data</para>
        /// </param>
        public static void FolderSet(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> pairs)
        {
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            string FolderPath = Path.Combine(AppConst.ApplicationPath, options.PersistencePath);
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
            foreach (var pair in pairs)
            {
                string FileName = Path.Combine(FolderPath, $"{pair.Key}.json");
                File.WriteAllText(FileName, AppRealization.JSON.Serialize(pair.Value));
            }
        }
    }
}
