using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.TraceLog;
using System.Reflection;
using unit.webapp.model.Dto;

namespace Air.Cloud.UnitTest.Modules.SkyWalking
{
    /// <summary>
    /// <para>zh-cn:SkyWalking 模块迁移测试集合。</para>
    /// <para>en-us:Migrated test suite for SkyWalking module behaviors.</para>
    /// </summary>
    public class SkyWalkingModuleTests
    {
        /// <summary>
        /// <para>zh-cn:验证 Test 方法会写入序列化后的 DTO 日志、调用 Taxin 并返回 DTO 与远程结果。</para>
        /// <para>en-us:Verifies that Test writes the serialized DTO log, invokes Taxin, and returns both the DTO and remote data.</para>
        /// </summary>
        [Fact]
        public async Task Test_should_write_trace_log_invoke_taxin_and_return_payload()
        {
            var dto = new TestSDto
            {
                Id = "trace-id",
                UserId = "user-02",
                ServiceNo = "svc-001",
                LoseTime = new DateTime(2026, 3, 25, 12, 30, 0, DateTimeKind.Utc)
            };
            var traceLog = new StubTraceLogStandard();
            var expectedPayload = new { result = "ok" };
            var taxinClient = new StubTaxinClientStandard
            {
                Handler = (route, _, _) =>
                {
                    Assert.Equal("taxin.service.test", route);
                    return Task.FromResult<object>(expectedPayload);
                }
            };
            var service = new unit.webapp.service.services.SkywalkingModuleTest.SkyWalkingModuleService(traceLog, taxinClient);

            var result = await service.Test(dto);
            var returnedDto = ReadProperty<TestSDto>(result, "dto");
            var returnedData = ReadProperty<object>(result, "data");

            Assert.Equal(AppRealization.JSON.Serialize(dto), traceLog.LastLogContent);
            Assert.Same(dto, returnedDto);
            Assert.Same(expectedPayload, returnedData);
            Assert.Equal(1, taxinClient.SendCallCount);
        }

        /// <summary>
        /// <para>zh-cn:按名称读取匿名对象属性值。</para>
        /// <para>en-us:Reads an anonymous object property value by its name.</para>
        /// </summary>
        /// <typeparam name="TValue">
        /// <para>zh-cn:目标属性值类型。</para>
        /// <para>en-us:The expected property value type.</para>
        /// </typeparam>
        /// <param name="instance">
        /// <para>zh-cn:要读取属性的对象实例。</para>
        /// <para>en-us:The object instance whose property should be read.</para>
        /// </param>
        /// <param name="propertyName">
        /// <para>zh-cn:目标属性名称。</para>
        /// <para>en-us:The target property name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回读取到的属性值。</para>
        /// <para>en-us:Returns the extracted property value.</para>
        /// </returns>
        private static TValue? ReadProperty<TValue>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            return (TValue?)property.GetValue(instance);
        }

        /// <summary>
        /// <para>zh-cn:TraceLog 最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation for the trace log dependency.</para>
        /// </summary>
        private sealed class StubTraceLogStandard : ITraceLogStandard
        {
            /// <summary>
            /// <para>zh-cn:记录最近一次写入的字符串日志内容。</para>
            /// <para>en-us:Tracks the latest string log content written.</para>
            /// </summary>
            public string? LastLogContent { get; private set; }

            /// <inheritdoc />
            public void Write(string logContent, IDictionary<string, string>? Tag = null)
            {
                LastLogContent = logContent;
            }

            /// <inheritdoc />
            public void Write(AppPrintInformation logContent, IDictionary<string, string>? Tag = null) => throw new NotSupportedException();

            /// <inheritdoc />
            public void Write<TLog>(TLog logContent, IDictionary<string, string>? Tag = null) where TLog : Air.Cloud.Core.Standard.TraceLog.ITraceLogContent, new() => throw new NotSupportedException();
        }

        /// <summary>
        /// <para>zh-cn:Taxin 客户端最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation for the Taxin client dependency.</para>
        /// </summary>
        private sealed class StubTaxinClientStandard : ITaxinClientStandard
        {
            /// <summary>
            /// <para>zh-cn:处理 SendAsync 调用的委托。</para>
            /// <para>en-us:Delegate used to handle SendAsync invocations.</para>
            /// </summary>
            public Func<string, object?, Tuple<Version, Version>?, Task<object>> Handler { get; set; } = (_, _, _) => Task.FromResult<object>(new object());

            /// <summary>
            /// <para>zh-cn:记录 SendAsync 的调用次数。</para>
            /// <para>en-us:Tracks how many times SendAsync was called.</para>
            /// </summary>
            public int SendCallCount { get; private set; }

            /// <inheritdoc />
            public async Task<TResult> SendAsync<TResult>(string Route, object? Data = null, Tuple<Version, Version>? Version = null) where TResult : class
            {
                SendCallCount++;
                var result = await Handler(Route, Data, Version);
                return result as TResult;
            }

            /// <inheritdoc />
            public Task PushAsync() => Task.CompletedTask;

            /// <inheritdoc />
            public Task PullAsync() => Task.CompletedTask;

            /// <inheritdoc />
            public Task CheckAsync() => Task.CompletedTask;

            /// <inheritdoc />
            public Task OnLineAsync() => Task.CompletedTask;

            /// <inheritdoc />
            public Task OffLineAsync() => Task.CompletedTask;
        }
    }
}
