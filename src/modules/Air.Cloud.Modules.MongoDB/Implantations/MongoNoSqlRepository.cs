using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Modules.MongoDB.Attributes;

using MongoDB.Driver;

using System.Reflection;

#nullable enable annotations

namespace Air.Cloud.Modules.MongoDB.Implantations
{
    /// <summary>
    /// <para>zh-cn:MongoDB的数据仓储</para>
    /// <para>en-us:MongoDB data repository</para>
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class MongoNoSqlRepository<TDocument> : INoSqlRepository<TDocument>
        where TDocument : class, INoSqlEntity, new()
    {
        private readonly MongoCollectionAttribute collectionAttribute;
        private readonly MongoDBConnection connection;
        private readonly IMongoCollection<TDocument> collection;
        private readonly IQueryable<TDocument> queryable;

        public MongoNoSqlRepository()
        {
            Type documentType = typeof(TDocument);
            collectionAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>()
                ?? throw new MongoDBException($"未检测到\"{documentType.Name}\"的MongoCollection特性");
            connection = new MongoDBConnection(documentType);
            collection = connection.Database.GetCollection<TDocument>(collectionAttribute.CollectionName);
            queryable = collection.AsQueryable();
        }

        /// <inheritdoc/>
        public INoSqlRepository<TDodument> Change<TDodument>() where TDodument : class, INoSqlEntity, new()
        {
            return AppCore.GetService<INoSqlRepository<TDodument>>();
        }

        /// <inheritdoc/>
        public TDocument Save(TDocument T)
        {
            ValidateDocument(T);
            collection.ReplaceOne(GetIdFilter(T.Id), T, new ReplaceOptions { IsUpsert = true });
            return T;
        }

        /// <inheritdoc/>
        public async Task<TDocument> SaveAsync(TDocument T)
        {
            ValidateDocument(T);
            await collection.ReplaceOneAsync(GetIdFilter(T.Id), T, new ReplaceOptions { IsUpsert = true });
            return T;
        }

        /// <inheritdoc/>
        public bool Save(IEnumerable<TDocument> T)
        {
            ValidateDocuments(T);
            var models = BuildReplaceModels(T, true);
            if (!models.Any()) return true;

            var response = collection.BulkWrite(models);
            return response.IsAcknowledged;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveAsync(IEnumerable<TDocument> T)
        {
            ValidateDocuments(T);
            var models = BuildReplaceModels(T, true);
            if (!models.Any()) return true;

            var response = await collection.BulkWriteAsync(models);
            return response.IsAcknowledged;
        }

        /// <inheritdoc/>
        public TDocument Update(TDocument T)
        {
            ValidateDocument(T);
            var response = collection.ReplaceOne(GetIdFilter(T.Id), T, new ReplaceOptions { IsUpsert = false });
            if (response.MatchedCount > 0)
            {
                return T;
            }

            throw new MongoDBException($"未找到需要更新的Mongo文档,Id:{T.Id}");
        }

        /// <inheritdoc/>
        public async Task<TDocument> UpdateAsync(TDocument T)
        {
            ValidateDocument(T);
            var response = await collection.ReplaceOneAsync(GetIdFilter(T.Id), T, new ReplaceOptions { IsUpsert = false });
            if (response.MatchedCount > 0)
            {
                return T;
            }

            throw new MongoDBException($"未找到需要更新的Mongo文档,Id:{T.Id}");
        }

        /// <inheritdoc/>
        public TDocument? FirstOrDefault(string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return null;
            }

            return collection.Find(GetIdFilter(Key)).FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<TDocument?> FirstOrDefaultAsync(string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return null;
            }

            return await collection.Find(GetIdFilter(Key)).FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public IQueryable<TDocument> Query(Func<INoSqlRepository<TDocument>, IQueryable<TDocument>> Query)
        {
            return Query.Invoke(new MongoNoSqlQueryRepository<TDocument>(this, queryable));
        }

        /// <inheritdoc/>
        public async Task<IQueryable<TDocument>> QueryAsync(Func<INoSqlRepository<TDocument>, Task<IQueryable<TDocument>>> Query)
        {
            return await Query.Invoke(new MongoNoSqlQueryRepository<TDocument>(this, queryable));
        }

        /// <inheritdoc/>
        public bool Delete(string Id = null)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return false;
            }

            var response = collection.DeleteOne(GetIdFilter(Id));
            return response.IsAcknowledged && response.DeletedCount > 0;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string Id = null)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return false;
            }

            var response = await collection.DeleteOneAsync(GetIdFilter(Id));
            return response.IsAcknowledged && response.DeletedCount > 0;
        }

        /// <inheritdoc/>
        public TClient Client<TClient>() where TClient : class
        {
            if (typeof(TClient) == typeof(IMongoCollection<TDocument>))
            {
                return collection as TClient;
            }

            if (typeof(TClient) == typeof(IMongoDatabase))
            {
                return connection.Database as TClient;
            }

            if (typeof(TClient) == typeof(IMongoClient))
            {
                return connection.Client as TClient;
            }

            if (typeof(TClient) == typeof(IQueryable<TDocument>))
            {
                return queryable as TClient;
            }

            return null;
        }

        private static FilterDefinition<TDocument> GetIdFilter(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Mongo文档Id不能为空", nameof(id));
            }

            return Builders<TDocument>.Filter.Eq(x => x.Id, id);
        }

        private static void ValidateDocument(TDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(document.Id))
            {
                throw new ArgumentException("Mongo文档Id不能为空", nameof(document));
            }
        }

        private static void ValidateDocuments(IEnumerable<TDocument> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            foreach (var document in documents)
            {
                ValidateDocument(document);
            }
        }

        private static List<WriteModel<TDocument>> BuildReplaceModels(IEnumerable<TDocument> documents, bool isUpsert)
        {
            return documents?
                .Select(item => new ReplaceOneModel<TDocument>(GetIdFilter(item.Id), item)
                {
                    IsUpsert = isUpsert
                })
                .Cast<WriteModel<TDocument>>()
                .ToList() ?? new List<WriteModel<TDocument>>();
        }

        private class MongoNoSqlQueryRepository<TQueryDocument> : INoSqlRepository<TQueryDocument>
            where TQueryDocument : class, INoSqlEntity, new()
        {
            private readonly INoSqlRepository<TQueryDocument> repository;
            private readonly IQueryable<TQueryDocument> queryable;

            public MongoNoSqlQueryRepository(INoSqlRepository<TQueryDocument> repository, IQueryable<TQueryDocument> queryable)
            {
                this.repository = repository;
                this.queryable = queryable;
            }

            public INoSqlRepository<TDodument> Change<TDodument>() where TDodument : class, INoSqlEntity, new()
            {
                return repository.Change<TDodument>();
            }

            public TQueryDocument Save(TQueryDocument T)
            {
                return repository.Save(T);
            }

            public Task<TQueryDocument> SaveAsync(TQueryDocument T)
            {
                return repository.SaveAsync(T);
            }

            public bool Save(IEnumerable<TQueryDocument> T)
            {
                return repository.Save(T);
            }

            public Task<bool> SaveAsync(IEnumerable<TQueryDocument> T)
            {
                return repository.SaveAsync(T);
            }

            public TQueryDocument Update(TQueryDocument T)
            {
                return repository.Update(T);
            }

            public Task<TQueryDocument> UpdateAsync(TQueryDocument T)
            {
                return repository.UpdateAsync(T);
            }

            public TQueryDocument? FirstOrDefault(string Key)
            {
                return repository.FirstOrDefault(Key);
            }

            public Task<TQueryDocument?> FirstOrDefaultAsync(string Key)
            {
                return repository.FirstOrDefaultAsync(Key);
            }

            public IQueryable<TQueryDocument> Query(Func<INoSqlRepository<TQueryDocument>, IQueryable<TQueryDocument>> Query)
            {
                return Query.Invoke(this);
            }

            public Task<IQueryable<TQueryDocument>> QueryAsync(Func<INoSqlRepository<TQueryDocument>, Task<IQueryable<TQueryDocument>>> Query)
            {
                return Query.Invoke(this);
            }

            public bool Delete(string Id = null)
            {
                return repository.Delete(Id);
            }

            public Task<bool> DeleteAsync(string Id = null)
            {
                return repository.DeleteAsync(Id);
            }

            public TClient Client<TClient>() where TClient : class
            {
                if (typeof(TClient) == typeof(IQueryable<TQueryDocument>))
                {
                    return queryable as TClient;
                }

                return repository.Client<TClient>();
            }
        }
    }
}
