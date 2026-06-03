using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Modules.ElasticSearch;
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Connections;
using Air.Cloud.Modules.ElasticSearch.Enums;
using Air.Cloud.Modules.ElasticSearch.Extensions;
using Air.Cloud.Modules.ElasticSearch.Implantations;

using Nest;

namespace Air.Cloud.IntegrationTest.Modules.ElasticSearch;

/// <summary>
/// <para>zh-cn:ElasticSearch 真实集群集成测试，开启后会连接配置中的 ES 集群并执行真实文档 CRUD。</para>
/// <para>en-us:Real ElasticSearch cluster integration tests. When enabled, they connect to the configured ES cluster and execute real document CRUD.</para>
/// </summary>
/// <remarks>
/// <para>zh-cn:默认通过 ElasticSearchIntegration:RunElasticSearchTests 关闭；开启后 ES 不可用应视为集成环境失败。</para>
/// <para>en-us:Disabled by ElasticSearchIntegration:RunElasticSearchTests by default; when enabled, unavailable ES should be treated as integration-environment failure.</para>
/// </remarks>
public class ElasticSearchRealClusterIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 ElasticSearch 模块仓储可以在真实 ES 集群完成保存、查询、更新和删除。</para>
    /// <para>en-us:Verifies the ElasticSearch module repository can save, query, update, and delete on a real ES cluster.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
    public async Task ElasticSearch_should_save_query_update_and_delete_document()
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = CreateElasticClient();
        RegisterRepositoryClient(client);
        INoSqlRepository<ElasticIntegrationDocument> repository = new ESNoSqlRepository<ElasticIntegrationDocument>();
        var document = new ElasticIntegrationDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Tags = "integration,created",
            Url = "/integration/es/create"
        };

        try
        {
            var saved = await repository.SaveAsync(document);
            Assert.Equal(document.Id, saved.Id);
            await client.Indices.RefreshAsync(GetIndexName());

            var loaded = await repository.FirstOrDefaultAsync(document.Id);
            Assert.NotNull(loaded);
            Assert.Equal(document.Url, loaded.Url);

            document.Tags = "integration,updated";
            document.Url = "/integration/es/update";
            var updated = await repository.UpdateAsync(document);
            Assert.Equal(document.Id, updated.Id);
            await client.Indices.RefreshAsync(GetIndexName());

            var queried = await repository.QueryAsync(async context =>
            {
                var response = await context.Client<IElasticClient>().SearchAsync<ElasticIntegrationDocument>(descriptor =>
                    descriptor.From(0)
                        .Size(10)
                        .Query(query => query.Term(term => term.Field(field => field.Id).Value(document.Id))));

                return response.Documents.AsQueryable();
            });

            Assert.Contains(queried, item => item.Id == document.Id);
        }
        finally
        {
            try
            {
                await repository.DeleteAsync(document.Id);
            }
            catch
            {
                // zh-cn:清理失败不应掩盖前面的真实断言结果。
                // en-us:Cleanup failure should not hide previous real assertion results.
            }
        }

        Assert.Null(await repository.FirstOrDefaultAsync(document.Id));
    }

    /// <summary>
    /// <para>zh-cn:验证真实 ElasticSearch 批量保存后，可以按文档 Id 查询回全部目标文档。</para>
    /// <para>en-us:Verifies all target documents can be queried by document id after batch save on a real ElasticSearch cluster.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
    public async Task ElasticSearch_should_save_batch_and_query_documents()
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = CreateElasticClient();
        RegisterRepositoryClient(client);
        INoSqlRepository<ElasticIntegrationDocument> repository = new ESNoSqlRepository<ElasticIntegrationDocument>();
        var documents = new[]
        {
            new ElasticIntegrationDocument { Id = Guid.NewGuid().ToString("N"), Tags = "batch", Url = "/integration/es/batch/1" },
            new ElasticIntegrationDocument { Id = Guid.NewGuid().ToString("N"), Tags = "batch", Url = "/integration/es/batch/2" }
        };

        try
        {
            Assert.True(await repository.SaveAsync(documents));
            await client.Indices.RefreshAsync(GetIndexName());

            var ids = documents.Select(document => document.Id).ToArray();
            var queried = await repository.QueryAsync(async context =>
            {
                var response = await context.Client<IElasticClient>().SearchAsync<ElasticIntegrationDocument>(descriptor =>
                    descriptor.From(0)
                        .Size(10)
                        .Query(query => query.Ids(idsQuery => idsQuery.Values(ids))));

                return response.Documents.AsQueryable();
            });

            Assert.Equal(2, queried.Count());
            Assert.Contains(queried, item => item.Id == documents[0].Id);
            Assert.Contains(queried, item => item.Id == documents[1].Id);
        }
        finally
        {
            foreach (var document in documents)
            {
                try
                {
                    await repository.DeleteAsync(document.Id);
                }
                catch
                {
                    // zh-cn:清理失败不应覆盖批量保存与查询的真实断言结果。
                    // en-us:Cleanup failure should not hide real batch save and query assertion results.
                }
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实 ElasticSearch 查询不存在的文档时返回 null，而不是抛出异常。</para>
    /// <para>en-us:Verifies querying a missing document from real ElasticSearch returns null instead of throwing.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
    public async Task ElasticSearch_should_return_null_when_document_does_not_exist()
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = CreateElasticClient();
        RegisterRepositoryClient(client);
        INoSqlRepository<ElasticIntegrationDocument> repository = new ESNoSqlRepository<ElasticIntegrationDocument>();

        Assert.Null(await repository.FirstOrDefaultAsync($"missing-{Guid.NewGuid():N}"));
    }

    /// <summary>
    /// <para>zh-cn:验证真实 ElasticSearch 删除不存在的文档会抛出异常，调用方不能把未命中删除当作成功。</para>
    /// <para>en-us:Verifies deleting a missing document from real ElasticSearch throws so callers cannot treat a miss as success.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
    public async Task ElasticSearch_should_throw_when_deleting_missing_document()
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = CreateElasticClient();
        RegisterRepositoryClient(client);
        INoSqlRepository<ElasticIntegrationDocument> repository = new ESNoSqlRepository<ElasticIntegrationDocument>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            repository.DeleteAsync($"missing-{Guid.NewGuid():N}"));
    }

    /// <summary>
    /// <para>zh-cn:验证真实 ElasticSearch 更新不存在的文档会抛出异常，避免业务误以为更新成功。</para>
    /// <para>en-us:Verifies updating a missing document in real ElasticSearch throws to prevent false update-success signals.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
    public async Task ElasticSearch_should_throw_when_updating_missing_document()
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = CreateElasticClient();
        RegisterRepositoryClient(client);
        INoSqlRepository<ElasticIntegrationDocument> repository = new ESNoSqlRepository<ElasticIntegrationDocument>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            repository.UpdateAsync(new ElasticIntegrationDocument
            {
                Id = $"missing-{Guid.NewGuid():N}",
                Tags = "missing",
                Url = "/integration/es/missing"
            }));
    }

    /// <summary>
    /// <para>zh-cn:验证 ElasticSearch 索引切分命名规则在集成测试层也保持稳定，避免模块配置生成错误索引名。</para>
    /// <para>en-us:Verifies ElasticSearch index segmentation naming rules remain stable at integration-test level to avoid wrong index names.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ElasticSearch")]
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

    private static IElasticClient CreateElasticClient()
    {
        var connectionString = GetRequiredConfiguration("ElasticSearchIntegration:ConnectionString");
        var indexName = GetIndexName();
        var nodes = connectionString
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => new Uri(item))
            .ToArray();
        var pool = new Elasticsearch.Net.StaticConnectionPool(nodes);
        var settings = new ConnectionSettings(pool).DefaultIndex(indexName);
        var username = AppConfigurationLoader.InnerConfiguration["ElasticSearchIntegration:UserName"];
        var password = AppConfigurationLoader.InnerConfiguration["ElasticSearchIntegration:Password"];

        if (!string.IsNullOrWhiteSpace(username))
        {
            settings.BasicAuthentication(username, password);
        }

        return new ElasticClient(settings);
    }

    private static void RegisterRepositoryClient(IElasticClient client)
    {
        var indexName = GetIndexName();
        var attribute = typeof(ElasticIntegrationDocument)
            .GetCustomAttributes(typeof(ElasticSearchIndexAttribute), inherit: false)
            .Cast<ElasticSearchIndexAttribute>()
            .Single();
        attribute.TableName = indexName;

        var element = new ElasticClientPoolElement(client, typeof(ElasticIntegrationDocument));
        ElasticSearchConnection.Pool.Set(element);
    }

    private static bool IsEnabled()
    {
        return string.Equals(
            AppConfigurationLoader.InnerConfiguration["ElasticSearchIntegration:RunElasticSearchTests"],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetIndexName()
    {
        var indexName = AppConfigurationLoader.InnerConfiguration["ElasticSearchIntegration:IndexName"];
        if (string.IsNullOrWhiteSpace(indexName))
        {
            indexName = $"air-cloud-it-{DateTime.UtcNow:yyyyMMdd}";
        }

        return indexName.ToLowerInvariant();
    }

    private static string GetRequiredConfiguration(string key)
    {
        var value = AppConfigurationLoader.InnerConfiguration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    [ElasticSearchIndex(TableName = "air-cloud-it-placeholder")]
    private sealed class ElasticIntegrationDocument : INoSqlEntity
    {
        public string Id { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;
    }
}
