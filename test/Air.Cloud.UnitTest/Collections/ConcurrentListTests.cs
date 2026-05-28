using Air.Cloud.Core.Collections;

namespace Air.Cloud.UnitTest.Collections
{
    public class ConcurrentListTests
    {
        /// <summary>
        /// <para>zh-cn:测试多线程单条写入场景，确认并发 Add 不会丢元素且总量准确。</para>
        /// <para>en-us:Tests concurrent single-item writes to ensure Add does not lose items and keeps an accurate total count.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：启动多个并发任务持续 Add，等待完成后校验最终 Count 与边界元素均存在。</para>
        /// <para>en-us:Process: run multiple parallel tasks performing Add operations, then verify final Count and boundary items.</para>
        /// </remarks>
        [Fact]
        public async Task Add_should_keep_all_items_under_multi_threading()
        {
            var list = new ConcurrentList<int>();
            const int workerCount = 8;
            const int itemsPerWorker = 1000;

            var tasks = Enumerable.Range(0, workerCount)
                .Select(worker => Task.Run(() =>
                {
                    var start = worker * itemsPerWorker;
                    for (var i = 0; i < itemsPerWorker; i++)
                    {
                        list.Add(start + i);
                    }
                }));

            await Task.WhenAll(tasks);

            Assert.Equal(workerCount * itemsPerWorker, list.Count);
            Assert.Contains(0, list);
            Assert.Contains(workerCount * itemsPerWorker - 1, list);
        }

        /// <summary>
        /// <para>zh-cn:测试多线程批量写入场景，确认并发 AddRange 后集合完整且计数一致。</para>
        /// <para>en-us:Tests concurrent batch writes to ensure AddRange keeps collection completeness and consistent count.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：并发提交多个区间批量数据，全部完成后校验总数与首尾元素命中。</para>
        /// <para>en-us:Process: submit multiple range batches in parallel, then verify total count and first/last item presence.</para>
        /// </remarks>
        [Fact]
        public async Task AddRange_should_keep_all_items_under_multi_threading()
        {
            var list = new ConcurrentList<int>();
            const int workerCount = 4;
            const int itemsPerWorker = 500;

            var tasks = Enumerable.Range(0, workerCount)
                .Select(worker => Task.Run(() =>
                {
                    var start = worker * itemsPerWorker;
                    var values = Enumerable.Range(start, itemsPerWorker);
                    list.AddRange(values);
                }));

            await Task.WhenAll(tasks);

            Assert.Equal(workerCount * itemsPerWorker, list.Count);
            Assert.Contains(0, list);
            Assert.Contains(workerCount * itemsPerWorker - 1, list);
        }

        /// <summary>
        /// <para>zh-cn:测试快照枚举场景，确认枚举期间写入不会阻塞且新元素不污染当前快照。</para>
        /// <para>en-us:Tests snapshot enumeration to ensure writers are not blocked and new items do not appear in current snapshot iteration.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先获取枚举器再异步写入新值，遍历快照并断言快照不含新值但集合最终包含新值。</para>
        /// <para>en-us:Process: obtain enumerator first, add a new value asynchronously, iterate snapshot, and verify snapshot exclusion with final collection inclusion.</para>
        /// </remarks>
        [Fact]
        public async Task Enumerator_should_use_snapshot_and_not_block_writer()
        {
            var list = new ConcurrentList<int>(new List<int> { 1, 2, 3 });

            var enumerator = ((IEnumerable<int>)list).GetEnumerator();

            var addTask = Task.Run(() => list.Add(4));
            await addTask.WaitAsync(TimeSpan.FromSeconds(2));

            var values = new List<int>();
            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            Assert.DoesNotContain(4, values);
            Assert.Contains(4, list);
        }

        /// <summary>
        /// <para>zh-cn:测试释放后保护场景，确认 Dispose 后访问写入与读取都抛出 ObjectDisposedException。</para>
        /// <para>en-us:Tests post-dispose protection to ensure both write and read access throw ObjectDisposedException after Dispose.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先释放实例，再分别调用 Add 与 Count，断言两条路径都触发已释放异常。</para>
        /// <para>en-us:Process: dispose instance first, then call Add and Count and assert both paths throw disposed exceptions.</para>
        /// </remarks>
        [Fact]
        public void Dispose_should_prevent_further_operations()
        {
            var list = new ConcurrentList<int>();
            list.Dispose();

            Assert.Throws<ObjectDisposedException>(() => list.Add(1));
            Assert.Throws<ObjectDisposedException>(() => _ = list.Count);
        }

        /// <summary>
        /// <para>zh-cn:测试重复释放幂等场景，确认多次调用 Dispose 不会重复抛错。</para>
        /// <para>en-us:Tests dispose idempotency to ensure repeated Dispose calls do not throw.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：创建实例后连续调用两次 Dispose，以无异常完成作为通过条件。</para>
        /// <para>en-us:Process: create instance and call Dispose twice consecutively, expecting completion without exception.</para>
        /// </remarks>
        [Fact]
        public void Dispose_called_multiple_times_should_not_throw()
        {
            var list = new ConcurrentList<int>();

            list.Dispose();
            list.Dispose();
        }
    }
}
