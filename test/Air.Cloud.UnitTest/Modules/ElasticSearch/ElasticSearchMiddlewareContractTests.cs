using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Modules.ElasticSearch.Connections;
using Air.Cloud.Modules.ElasticSearch.Enums;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch 中间件仓储契约单元测试，方法名与真实 ES 集成测试保持同步。</para>
    /// <para>en-us:ElasticSearch middleware repository contract unit tests with method names synchronized with real ES integration tests.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该类使用内存 NoSQL 仓储验证 Save/Get/Query/Update/Delete 调用链；真实 ES 行为由同名集成测试验证。</para>
    /// <para>en-us:This class uses an in-memory NoSQL repository to verify Save/Get/Query/Update/Delete flows; real ES behavior is verified by same-named integration tests.</para>
    /// </remarks>
    public class ElasticSearchMiddlewareContractTests
    {
        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证文档保存、查询、更新和删除契约。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying document save, query, update, and delete contracts.</para>
        /// </summary>
        [Fact]
        public async Task ElasticSearch_should_save_query_update_and_delete_document()
        {
            INoSqlRepository<ElasticContractDocument> repository = new InMemoryNoSqlRepository<ElasticContractDocument>();
            var document = new ElasticContractDocument
            {
                Id = "doc-1",
                Tags = "integration,created",
                Url = "/integration/es/create"
            };

            var saved = await repository.SaveAsync(document);
            Assert.Equal(document.Id, saved.Id);

            var loaded = await repository.FirstOrDefaultAsync(document.Id);
            Assert.NotNull(loaded);
            Assert.Equal(document.Url, loaded.Url);

            document.Tags = "integration,updated";
            document.Url = "/integration/es/update";
            var updated = await repository.UpdateAsync(document);
            Assert.Equal(document.Id, updated.Id);

            var queried = await repository.QueryAsync(context =>
                Task.FromResult(context.Client<IQueryable<ElasticContractDocument>>()
                    .Where(item => item.Id == document.Id)));

            Assert.Contains(queried, item => item.Id == document.Id && item.Tags == "integration,updated");

            Assert.True(await repository.DeleteAsync(document.Id));
            Assert.Null(await repository.FirstOrDefaultAsync(document.Id));
        }

        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证批量保存后可以按查询条件取回所有目标文档。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying all target documents can be queried after batch save.</para>
        /// </summary>
        [Fact]
        public async Task ElasticSearch_should_save_batch_and_query_documents()
        {
            INoSqlRepository<ElasticContractDocument> repository = new InMemoryNoSqlRepository<ElasticContractDocument>();
            var documents = new[]
            {
                new ElasticContractDocument { Id = "batch-1", Tags = "batch", Url = "/integration/es/batch/1" },
                new ElasticContractDocument { Id = "batch-2", Tags = "batch", Url = "/integration/es/batch/2" }
            };

            Assert.True(await repository.SaveAsync(documents));

            var queried = await repository.QueryAsync(context =>
                Task.FromResult(context.Client<IQueryable<ElasticContractDocument>>()
                    .Where(item => item.Tags == "batch")));

            Assert.Equal(2, queried.Count());
            Assert.Contains(queried, item => item.Id == "batch-1");
            Assert.Contains(queried, item => item.Id == "batch-2");
        }

        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证查询不存在的文档返回 null，而不是抛出异常。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying querying a missing document returns null instead of throwing.</para>
        /// </summary>
        [Fact]
        public async Task ElasticSearch_should_return_null_when_document_does_not_exist()
        {
            INoSqlRepository<ElasticContractDocument> repository = new InMemoryNoSqlRepository<ElasticContractDocument>();

            Assert.Null(await repository.FirstOrDefaultAsync("missing-document"));
        }

        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证删除不存在的文档会抛出异常，调用方不能把它当作删除成功。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying deleting a missing document throws so callers cannot treat it as success.</para>
        /// </summary>
        [Fact]
        public async Task ElasticSearch_should_throw_when_deleting_missing_document()
        {
            INoSqlRepository<ElasticContractDocument> repository = new InMemoryNoSqlRepository<ElasticContractDocument>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.DeleteAsync("missing-document"));
        }

        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证更新不存在的文档会抛出异常，避免静默创建错误数据。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying updating a missing document throws to avoid silently creating wrong data.</para>
        /// </summary>
        [Fact]
        public async Task ElasticSearch_should_throw_when_updating_missing_document()
        {
            INoSqlRepository<ElasticContractDocument> repository = new InMemoryNoSqlRepository<ElasticContractDocument>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.UpdateAsync(new ElasticContractDocument
                {
                    Id = "missing-document",
                    Tags = "missing",
                    Url = "/integration/es/missing"
                }));
        }

        /// <summary>
        /// <para>zh-cn:与 ElasticSearch 集成测试同名，验证索引切分命名规则可生成稳定的年、月、日索引名称。</para>
        /// <para>en-us:Same name as the ElasticSearch integration test, verifying index segmentation naming rules generate stable yearly, monthly, and daily index names.</para>
        /// </summary>
        [Fact]
        public void ElasticSearch_index_name_should_follow_segmentation_rules()
        {
            var now = DateTime.Now;

            Assert.Equal(
                $"air-cloud-it-{now.Year}",
                ElasticClientPoolElement.GetTableName("air-cloud-it", IndexSegmentationPatternEnum.Year));
            Assert.Equal(
                $"air-cloud-it-{now.Year - 2000}-{now.Month}",
                ElasticClientPoolElement.GetTableName("air-cloud-it", IndexSegmentationPatternEnum.Month));
            Assert.Equal(
                $"air-cloud-it-{now.Year - 2000}-{now.DayOfYear}",
                ElasticClientPoolElement.GetTableName("air-cloud-it", IndexSegmentationPatternEnum.Day));
        }

        private sealed class ElasticContractDocument : INoSqlEntity
        {
            public string Id { get; set; } = string.Empty;

            public string Tags { get; set; } = string.Empty;

            public string Url { get; set; } = string.Empty;
        }

        private sealed class InMemoryNoSqlRepository<TDocument> : INoSqlRepository<TDocument>
            where TDocument : class, INoSqlEntity, new()
        {
            private readonly Dictionary<string, TDocument> _documents = new();

            public INoSqlRepository<TDodument> Change<TDodument>()
                where TDodument : class, INoSqlEntity, new()
            {
                return new InMemoryNoSqlRepository<TDodument>();
            }

            public TDocument Save(TDocument T)
            {
                _documents[T.Id] = Clone(T);
                return T;
            }

            public Task<TDocument> SaveAsync(TDocument T)
            {
                return Task.FromResult(Save(T));
            }

            public bool Save(IEnumerable<TDocument> T)
            {
                foreach (var document in T)
                {
                    Save(document);
                }

                return true;
            }

            public Task<bool> SaveAsync(IEnumerable<TDocument> T)
            {
                return Task.FromResult(Save(T));
            }

            public TDocument Update(TDocument T)
            {
                if (!_documents.ContainsKey(T.Id))
                {
                    throw new InvalidOperationException($"Document '{T.Id}' does not exist.");
                }

                _documents[T.Id] = Clone(T);
                return T;
            }

            public Task<TDocument> UpdateAsync(TDocument T)
            {
                return Task.FromResult(Update(T));
            }

            public bool Delete(string Id = null!)
            {
                if (string.IsNullOrWhiteSpace(Id))
                {
                    return false;
                }

                if (_documents.Remove(Id))
                {
                    return true;
                }

                throw new InvalidOperationException($"Document '{Id}' does not exist.");
            }

            public Task<bool> DeleteAsync(string Id = null!)
            {
                return Task.FromResult(Delete(Id));
            }

            public TDocument? FirstOrDefault(string Key)
            {
                return _documents.TryGetValue(Key, out var document) ? Clone(document) : null;
            }

            public Task<TDocument?> FirstOrDefaultAsync(string Key)
            {
                return Task.FromResult(FirstOrDefault(Key));
            }

            public IQueryable<TDocument> Query(Func<INoSqlRepository<TDocument>, IQueryable<TDocument>> Query)
            {
                return Query.Invoke(this);
            }

            public Task<IQueryable<TDocument>> QueryAsync(Func<INoSqlRepository<TDocument>, Task<IQueryable<TDocument>>> Query)
            {
                return Query.Invoke(this);
            }

            public TClient Client<TClient>()
                where TClient : class
            {
                if (typeof(TClient) == typeof(IQueryable<TDocument>))
                {
                    return _documents.Values.Select(Clone).AsQueryable() as TClient
                        ?? throw new InvalidOperationException("Unable to cast in-memory queryable client.");
                }

                throw new InvalidOperationException($"Client type '{typeof(TClient).FullName}' is not supported by the in-memory repository.");
            }

            private static TDocument Clone(TDocument document)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(document);
                return System.Text.Json.JsonSerializer.Deserialize<TDocument>(json)!;
            }
        }
    }
}
