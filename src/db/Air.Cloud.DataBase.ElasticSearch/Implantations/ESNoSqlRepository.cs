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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Connections;
using Air.Cloud.DataBase.ElasticSearch.Extensions;

using System.Reflection;

namespace Air.Cloud.DataBase.ElasticSearch.Implantations
{
    /// <summary>
    /// <para>zh-cn:ES的数据仓储</para>
    /// <para>en-us:ES data repository</para>
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class ESNoSqlRepository<TDocument> : INoSqlRepository<TDocument>
        where TDocument :class,INoSqlEntity, new()
    {
        /// <summary>
        /// <para>zh-cn:索引名称</para>
        /// </summary>
        private readonly ElasticClientPoolElement clientPoolElement;
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us:Constractor</para>
        /// </summary>
        public ESNoSqlRepository()
        {
            Type DocumentType = typeof(TDocument);
            string Key = DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>().GetElementUID(DocumentType);
            clientPoolElement = ElasticSearchConnection.Pool.Get(Key);
            if (clientPoolElement==null)
            {
                //表示当前对象未被注册到池中 只有一种可能就是已经触发了滚动条件
                clientPoolElement= new ElasticClientPoolElement(DocumentType);
                ElasticSearchConnection.Pool.Set(clientPoolElement);
            }
        }
        /// <inheritdoc/>
        public INoSqlRepository<TDodument> Change<TDodument>() where TDodument : class,INoSqlEntity, new()
        {
            return (INoSqlRepository<TDodument>)AppCore.GetService<INoSqlRepository<TDocument>>();
        }
        /// <inheritdoc/>
        public TDocument Save(TDocument T)
        {
            IndexResponse response = clientPoolElement?.Client?.IndexDocument<TDocument>(T);
            if (response!=null&&response?.Result == Result.Created || response?.Result == Result.Updated)
            {
                return T;
            }
            throw response.OriginalException;
        }
        /// <inheritdoc/>
        public async Task<TDocument> SaveAsync(TDocument T)
        {
            IndexResponse response = await clientPoolElement?.Client?.IndexDocumentAsync<TDocument>(T);
            if (response != null && response?.Result == Result.Created || response?.Result == Result.Updated)
            {
                return T;
            }
            throw response.OriginalException;
        }
        /// <inheritdoc/>
        public bool Save(IEnumerable<TDocument> T)
        {
            BulkResponse response = clientPoolElement?.Client.IndexMany<TDocument>(T);
            if (response != null && response?.IsValid == true)
            {
                return true;
            }
            return false;
        }
        /// <inheritdoc/>
        public async  Task<bool> SaveAsync(IEnumerable<TDocument> T)
        {
            BulkResponse response = await clientPoolElement?.Client.IndexManyAsync<TDocument>(T);
            if (response != null && response?.IsValid == true)
            {
                return true;
            }
            return false;
        }
        /// <inheritdoc/>
        public TDocument Update(TDocument T)
        {
            DocumentPath<TDocument> deletePath = new DocumentPath<TDocument>(T.Id);
            var res = clientPoolElement?.Client.Update(deletePath, (p) => p.Doc(T).Index(clientPoolElement?.Client.ConnectionSettings.DefaultIndex));
            if (res!=null&& res.IsValid==true) return res.Get.Source;
            throw res.OriginalException;
        }
        /// <inheritdoc/>
        public async Task<TDocument> UpdateAsync(TDocument T)
        {
            DocumentPath<TDocument> deletePath = new DocumentPath<TDocument>(T.Id);
            var res = await clientPoolElement?.Client.UpdateAsync(deletePath, (p) => p.Doc(T).Index(clientPoolElement?.Client.ConnectionSettings.DefaultIndex));
            if (res != null && res.IsValid == true)
            {
                return T;
            }
            throw res.OriginalException;
        }
        /// <inheritdoc/>
        TDocument INoSqlRepository<TDocument>.FirstOrDefault(string Key)
        {
            return clientPoolElement?.Client.Get(new DocumentPath<TDocument>(new Id(Key))).Source;
        }
        /// <inheritdoc/>
        public async Task<TDocument?> FirstOrDefaultAsync(string Key)
        {
            return (await clientPoolElement?.Client.GetAsync(new DocumentPath<TDocument>(new Id(Key)))).Source;
        }
        /// <inheritdoc/>
        public IQueryable<TDocument> Query(Func<INoSqlRepository<TDocument>, IQueryable<TDocument>> Query)
        {
            return Query.Invoke(this);
        }
        /// <inheritdoc/>
        public async Task<IQueryable<TDocument>> QueryAsync(Func<INoSqlRepository<TDocument>, Task<IQueryable<TDocument>>> Query)
        {
            //由于查询方法相较于普通数据库来说复杂,所以查询语句由外部构建 并执行
            return await Query.Invoke(this);
        }
        /// <inheritdoc/>
        public bool Delete(string Id = null)
        {
            var path = DocumentPath<TDocument>.Id(Id).Index(clientPoolElement?.Client.ConnectionSettings.DefaultIndex);
            DeleteResponse response = clientPoolElement?.Client.Delete(path);
            if (response.Result == Result.Deleted) return true;
            throw response.OriginalException;
        }
        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string Id = null)
        {
            var path = DocumentPath<TDocument>.Id(Id).Index(clientPoolElement?.Client.ConnectionSettings.DefaultIndex);
            DeleteResponse response = await clientPoolElement?.Client.DeleteAsync(path);
            if (response.Result == Result.Deleted) return true;
            throw response.OriginalException;

        }
        /// <inheritdoc/>
        public TClient Client<TClient>() where TClient : class
        {
            return clientPoolElement?.Client as TClient;
        }
    }
}
