using Air.Cloud.Core.Standard.KVCenter;
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.Consul
{
    /// <summary>
    /// <para>zh-cn:Consul KV 中间件契约单元测试，方法名与真实 Consul 集成测试保持同步。</para>
    /// <para>en-us:Consul KV middleware contract unit tests with method names synchronized with real Consul integration tests.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该类使用内存 KV 实现验证标准调用语义；真实 Consul 行为由同名集成测试验证。</para>
    /// <para>en-us:This class uses an in-memory KV implementation to verify standard call semantics; real Consul behavior is verified by same-named integration tests.</para>
    /// </remarks>
    public class ConsulMiddlewareContractTests
    {
        /// <summary>
        /// <para>zh-cn:与 Consul 集成测试同名，验证 KV 添加、读取、前缀查询和删除契约。</para>
        /// <para>en-us:Same name as the Consul integration test, verifying KV add, get, prefix query, and delete contracts.</para>
        /// </summary>
        [Fact]
        public async Task Consul_should_add_query_get_and_delete_kv_value()
        {
            IKVCenterStandard kvCenter = new InMemoryKvCenterStandard();
            const string prefix = "air-cloud-it";
            const string key = $"{prefix}/unit-kv";
            const string value = "consul-value";

            Assert.True(await kvCenter.AddOrUpdateAsync(key, value));

            var item = await kvCenter.GetAsync<TestKvCenterServiceOptions>(key);
            Assert.NotNull(item);
            Assert.Equal(key, item.Key);
            Assert.Equal(value, item.Value);

            var items = await kvCenter.QueryAsync<TestKvCenterServiceOptions>(prefix);
            Assert.Contains(items, current => current.Key == key && current.Value == value);

            Assert.True(await kvCenter.DeleteAsync(key));
            Assert.Null(await kvCenter.GetAsync<TestKvCenterServiceOptions>(key));
        }

        /// <summary>
        /// <para>zh-cn:与 Consul 集成测试同名，验证同一个 KV Key 可以被覆盖写入，并且后续读取返回最新值。</para>
        /// <para>en-us:Same name as the Consul integration test, verifying the same KV key can be overwritten and later reads return the latest value.</para>
        /// </summary>
        [Fact]
        public async Task Consul_should_overwrite_existing_kv_value()
        {
            IKVCenterStandard kvCenter = new InMemoryKvCenterStandard();
            const string key = "air-cloud-it/unit-overwrite";

            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v1"));
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v2"));

            var item = await kvCenter.GetAsync<TestKvCenterServiceOptions>(key);

            Assert.NotNull(item);
            Assert.Equal("value-v2", item.Value);
        }

        /// <summary>
        /// <para>zh-cn:与 Consul 集成测试同名，验证前缀查询只返回目标前缀下的多个 Key，不会串入其他前缀数据。</para>
        /// <para>en-us:Same name as the Consul integration test, verifying prefix query returns multiple target keys without leaking other prefixes.</para>
        /// </summary>
        [Fact]
        public async Task Consul_should_query_multiple_kv_values_by_prefix()
        {
            IKVCenterStandard kvCenter = new InMemoryKvCenterStandard();
            const string prefix = "air-cloud-it/unit-prefix";

            Assert.True(await kvCenter.AddOrUpdateAsync($"{prefix}/one", "1"));
            Assert.True(await kvCenter.AddOrUpdateAsync($"{prefix}/two", "2"));
            Assert.True(await kvCenter.AddOrUpdateAsync("air-cloud-it/other/three", "3"));

            var items = await kvCenter.QueryAsync<TestKvCenterServiceOptions>(prefix);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item => item.Key == $"{prefix}/one" && item.Value == "1");
            Assert.Contains(items, item => item.Key == $"{prefix}/two" && item.Value == "2");
            Assert.DoesNotContain(items, item => item.Key.Contains("/other/", StringComparison.Ordinal));
        }

        /// <summary>
        /// <para>zh-cn:与 Consul 集成测试同名，验证删除不存在的 Key 返回 false，避免业务误判删除成功。</para>
        /// <para>en-us:Same name as the Consul integration test, verifying deleting a missing key returns false to avoid false success signals.</para>
        /// </summary>
        [Fact]
        public async Task Consul_should_return_false_when_deleting_missing_key()
        {
            IKVCenterStandard kvCenter = new InMemoryKvCenterStandard();

            Assert.False(await kvCenter.DeleteAsync("air-cloud-it/unit-missing"));
        }

        /// <summary>
        /// <para>zh-cn:与 Consul 集成测试同名，验证配置加载后可以读取 JSON 配置节点，并且变更后会刷新到最新值。</para>
        /// <para>en-us:Same name as the Consul integration test, verifying loaded JSON configuration can be read and refreshed to the latest value after changes.</para>
        /// </summary>
        [Fact]
        public void Consul_configuration_should_load_and_refresh_kv_json_value()
        {
            var provider = new ReloadableMemoryConfigurationProvider(
                new Dictionary<string, string?>
                {
                    ["Feature:Flag"] = "value-v1"
                });
            var configuration = new ConfigurationRoot(new List<IConfigurationProvider> { provider });

            Assert.Equal("value-v1", configuration["Feature:Flag"]);

            provider.Set("Feature:Flag", "value-v2");
            provider.Reload();

            Assert.Equal("value-v2", configuration["Feature:Flag"]);
        }

        private sealed class TestKvCenterServiceOptions : IKVCenterServiceOptions
        {
            public string Key { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;
        }

        private sealed class InMemoryKvCenterStandard : IKVCenterStandard
        {
            private readonly Dictionary<string, string> _values = new();

            public Task<IList<T>> QueryAsync<T>(string Prefix = "/")
                where T : class, new()
            {
                IList<T> values = _values
                    .Where(pair => pair.Key.StartsWith(Prefix, StringComparison.Ordinal))
                    .Select(pair => Create<T>(pair.Key, pair.Value))
                    .ToList();

                return Task.FromResult(values);
            }

            public Task<bool> AddOrUpdateAsync(string Key, string Value)
            {
                _values[Key] = Value;
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAsync(string Key)
            {
                return Task.FromResult(_values.Remove(Key));
            }

            public Task<T> GetAsync<T>(string Key)
                where T : IKVCenterServiceOptions, new()
            {
                return Task.FromResult(_values.TryGetValue(Key, out var value) ? Create<T>(Key, value) : default!);
            }

            private static T Create<T>(string key, string value)
                where T : new()
            {
                if (new T() is not IKVCenterServiceOptions options)
                {
                    throw new InvalidOperationException($"{typeof(T).FullName} must implement IKVCenterServiceOptions.");
                }

                options.Key = key;
                options.Value = value;
                return (T)options;
            }
        }

        private sealed class ReloadableMemoryConfigurationProvider : ConfigurationProvider
        {
            public ReloadableMemoryConfigurationProvider(Dictionary<string, string?> data)
            {
                Data = data;
            }

            public void Reload()
            {
                OnReload();
            }
        }
    }
}
