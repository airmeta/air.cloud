using Air.Cloud.Core.Modules.AppPool;
using System.Collections.Concurrent;

namespace Air.Cloud.UnitTest.Modules.AppPool
{
    public class AppPoolExtensionsTests
    {
        /// <summary>
        /// <para>zh-cn:测试命中查询场景，确认池中存在键时 IsExists 返回 true。</para>
        /// <para>en-us:Tests hit-query behavior to ensure IsExists returns true when the key exists in pool.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先写入一个键值对象，再调用 IsExists 检查相同键并断言命中。</para>
        /// <para>en-us:Process: insert one item first, call IsExists with the same key, and assert hit.</para>
        /// </remarks>
        [Fact]
        public void IsExists_should_return_true_when_key_exists()
        {
            var pool = new FakeAppPool();
            pool.Set(new TestItem { Key = "k1", Value = "v1" });

            var exists = pool.IsExists("k1");

            Assert.True(exists);
        }

        /// <summary>
        /// <para>zh-cn:测试未命中查询场景，确认池中不存在键时 IsExists 返回 false。</para>
        /// <para>en-us:Tests miss-query behavior to ensure IsExists returns false when key does not exist.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：不写入目标键直接调用 IsExists，断言返回 false。</para>
        /// <para>en-us:Process: call IsExists without inserting target key and assert false result.</para>
        /// </remarks>
        [Fact]
        public void IsExists_should_return_false_when_key_not_exists()
        {
            var pool = new FakeAppPool();

            var exists = pool.IsExists("missing");

            Assert.False(exists);
        }

        /// <summary>
        /// <para>zh-cn:测试清空语义场景，确认 Clear 后历史键全部失效。</para>
        /// <para>en-us:Tests clear semantics to ensure all previously inserted keys become unavailable after Clear.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先写入两个键后执行 Clear，再逐个检查 IsExists 都为 false。</para>
        /// <para>en-us:Process: insert two keys, execute Clear, then verify IsExists is false for both keys.</para>
        /// </remarks>
        [Fact]
        public void Clear_should_remove_all_items()
        {
            var pool = new FakeAppPool();
            pool.Set(new TestItem { Key = "k1", Value = "v1" });
            pool.Set(new TestItem { Key = "k2", Value = "v2" });

            pool.Clear();

            Assert.False(pool.IsExists("k1"));
            Assert.False(pool.IsExists("k2"));
        }

        private sealed class FakeAppPool : IAppPool<TestItem>
        {
            private readonly ConcurrentDictionary<string, TestItem> items = new();

            public TestItem Get(string Key)
            {
                items.TryGetValue(Key, out var value);
                return value!;
            }

            public bool Set(TestItem @object)
            {
                items[@object.Key] = @object;
                return true;
            }

            public bool Remove(string Key)
            {
                return items.TryRemove(Key, out _);
            }

            public void Clear()
            {
                items.Clear();
            }
        }

        private sealed class TestItem
        {
            public string Key { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;
        }
    }
}
