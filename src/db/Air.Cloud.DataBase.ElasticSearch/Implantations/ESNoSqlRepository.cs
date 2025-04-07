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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.DataBase.ElasticSearch.Attributes;

using System.Reflection;

namespace Air.Cloud.DataBase.ElasticSearch.Implantations
{
    public class ESNoSqlRepository<TDocument> : INoSqlRepository<TDocument>
        where TDocument :class,INoSqlEntity, new()
    {
        /// <summary>
        /// <para>zh-cn:索引名称</para>
        /// </summary>
        private readonly string IndexName;
        public ElasticSearchConnection DataBase;
        private DataBaseOptions Options => AppCore.GetOptions<DataBaseOptions>();
        public ESNoSqlRepository()
        {
            IndexName = typeof(TDocument).GetCustomAttribute<ElasticSearchIndexAttribute>()?.TableName;
            if(IndexName.IsNullOrEmpty()) IndexName=typeof(TDocument).Name.ToLower();
            DataBase=new ElasticSearchConnection()
            {
                ConnectionName = IndexName
            };
        }

        public INoSqlRepository<TDodument> Change<TDodument>() where TDodument : class,INoSqlEntity, new()
        {
            return (INoSqlRepository<TDodument>)AppCore.GetService<INoSqlRepository<TDocument>>();
        }
        public TDocument Save(TDocument T)
        {
            IndexResponse response = DataBase?.Connection?.IndexDocument<TDocument>(T);
            if (response!=null&&response?.Result == Result.Created || response?.Result == Result.Updated)
            {
                return T;
            }
            throw response.OriginalException;
        }

        public async Task<TDocument> SaveAsync(TDocument T)
        {
            IndexResponse response = await DataBase?.Connection?.IndexDocumentAsync<TDocument>(T);
            if (response != null && response?.Result == Result.Created || response?.Result == Result.Updated)
            {
                return T;
            }
            throw response.OriginalException;
        }

        public bool Save(IEnumerable<TDocument> T)
        {
            BulkResponse response = DataBase?.Connection.IndexMany<TDocument>(T);
            if (response != null && response?.IsValid == true)
            {
                return true;
            }
            return false;
        }

        public async  Task<bool> SaveAsync(IEnumerable<TDocument> T)
        {
            BulkResponse response = await DataBase?.Connection.IndexManyAsync<TDocument>(T);
            if (response != null && response?.IsValid == true)
            {
                return true;
            }
            return false;
        }

        public TDocument Update(TDocument T)
        {
            DocumentPath<TDocument> deletePath = new DocumentPath<TDocument>(T.Id);
            var res = DataBase.Connection.Update(deletePath, (p) => p.Doc(T).Index(DataBase.Connection.ConnectionSettings.DefaultIndex));
            if (res!=null&& res.IsValid==true) return res.Get.Source;
            throw res.OriginalException;
        }

        public async Task<TDocument> UpdateAsync(TDocument T)
        {
            DocumentPath<TDocument> deletePath = new DocumentPath<TDocument>(T.Id);
            var res = await DataBase.Connection.UpdateAsync(deletePath, (p) => p.Doc(T).Index(DataBase.Connection.ConnectionSettings.DefaultIndex));
            if (res != null && res.IsValid == true)
            {
                return T;
            }
            throw res.OriginalException;
        }

        TDocument INoSqlRepository<TDocument>.FirstOrDefault(string Key)
        {
            return DataBase.Connection.Get(new DocumentPath<TDocument>(new Id(Key))).Source;
        }

        public  async Task<TDocument?> FirstOrDefaultAsync(string Key)
        {
            return (await DataBase.Connection.GetAsync(new DocumentPath<TDocument>(new Id(Key)))).Source;
        }

        public IQueryable<TDocument> Query(Func<INoSqlRepository<TDocument>, IQueryable<TDocument>> Query)
        {
            return Query.Invoke(this);
        }

        public async Task<IQueryable<TDocument>> QueryAsync(Func<INoSqlRepository<TDocument>, Task<IQueryable<TDocument>>> Query)
        {
            //由于查询方法相较于普通数据库来说复杂,所以查询语句由外部构建 并执行
            return await Query.Invoke(this);
        }
        public bool Delete(string Id = null)
        {
            if(this.DataBase.Connection==null) throw new Exception("Connection Error");
            var path = DocumentPath<TDocument>.Id(Id).Index(this.DataBase.Connection.ConnectionSettings.DefaultIndex);
            DeleteResponse response = DataBase.Connection.Delete(path);
            if (response.Result == Result.Deleted) return true;
            throw response.OriginalException;
        }

        public async Task<bool> DeleteAsync(string Id = null)
        {
            if (this.DataBase.Connection == null) throw new Exception("Connection Error");
            var path = DocumentPath<TDocument>.Id(Id).Index(this.DataBase.Connection.ConnectionSettings.DefaultIndex);
            DeleteResponse response = await this.DataBase.Connection.DeleteAsync(path);
            if (response.Result == Result.Deleted) return true;
            throw response.OriginalException;

        }

        public TConnection Connection<TConnection>() where TConnection:class
        {
            return DataBase.Connection as TConnection;
        }
    }
}
