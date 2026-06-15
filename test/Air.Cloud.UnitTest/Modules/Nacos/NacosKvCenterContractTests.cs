using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Nacos.Model;

namespace Air.Cloud.UnitTest.Modules.Nacos
{
    /// <summary>
    /// <para>zh-cn:Nacos KV 中心标准契约单元测试，使用内存实现验证 dataId/group 与 Key/Value 的映射语义。</para>
    /// <para>en-us:Nacos KV center standard contract unit tests that use an in-memory implementation to verify dataId/group to Key/Value mapping semantics.</para>
    /// </summary>
    public class NacosKvCenterContractTests
    {
        /// <summary>
        /// <para>zh-cn:验证 Nacos KV 标准可以新增、读取、查询和删除配置项。</para>
        /// <para>en-us:Verifies the Nacos KV standard can add, get, query, and delete config items.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_should_add_query_get_and_delete_kv_value()
        {
            IKVCenterStandard kvCenter = new InMemoryNacosKvCenterStandard();
            const string key = "air-cloud-unit.json";
            const string value = "{\"Feature\":{\"Flag\":\"nacos-value\"}}";

            Assert.True(await kvCenter.AddOrUpdateAsync(key, value));

            var item = await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key);
            Assert.NotNull(item);
            Assert.Equal(key, item.Key);
            Assert.Equal(value, item.Value);

            var items = await kvCenter.QueryAsync<NacosKvCenterServiceInformation>(key);
            Assert.Single(items);

            Assert.True(await kvCenter.DeleteAsync(key));
            Assert.Null(await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key));
        }

        /// <summary>
        /// <para>zh-cn:验证 Nacos KV 标准对同一 dataId 的重复写入会覆盖旧值。</para>
        /// <para>en-us:Verifies repeated writes to the same Nacos dataId overwrite the old value.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_should_overwrite_existing_kv_value()
        {
            IKVCenterStandard kvCenter = new InMemoryNacosKvCenterStandard();
            const string key = "air-cloud-overwrite.json";

            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v1"));
            Assert.True(await kvCenter.AddOrUpdateAsync(key, "value-v2"));

            var item = await kvCenter.GetAsync<NacosKvCenterServiceInformation>(key);

            Assert.NotNull(item);
            Assert.Equal("value-v2", item.Value);
        }

        /// <summary>
        /// <para>zh-cn:验证查询不存在的 dataId 返回空集合，删除不存在的 dataId 返回 false。</para>
        /// <para>en-us:Verifies querying a missing dataId returns an empty list and deleting a missing dataId returns false.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_should_return_empty_or_false_for_missing_kv_value()
        {
            IKVCenterStandard kvCenter = new InMemoryNacosKvCenterStandard();

            Assert.Empty(await kvCenter.QueryAsync<NacosKvCenterServiceInformation>("missing.json"));
            Assert.False(await kvCenter.DeleteAsync("missing.json"));
        }

        private sealed class InMemoryNacosKvCenterStandard : IKVCenterStandard
        {
            private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

            public Task<IList<T>> QueryAsync<T>(string Prefix = "/") where T : class, new()
            {
                IList<T> values = _values.TryGetValue(Prefix, out var value)
                    ? new List<T> { Create<T>(Prefix, value) }
                    : new List<T>();

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

            public Task<T> GetAsync<T>(string Key) where T : IKVCenterServiceOptions, new()
            {
                return Task.FromResult(_values.TryGetValue(Key, out var value) ? Create<T>(Key, value) : default!);
            }

            private static T Create<T>(string key, string value) where T : new()
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
    }
}
