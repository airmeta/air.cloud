using Air.Cloud.Core.Standard.Taxin.Client;
using System.Reflection;
using Microsoft.AspNetCore.Routing;

using Air.Cloud.UnitTest.Compatibility.Services;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 客户端调用行为测试集合。</para>
    /// <para>en-us:Test suite for Taxin client invocation behaviors.</para>
    /// </summary>
    public class TaxinClientTests
    {
        /// <summary>
        /// <para>zh-cn:测试 ClientA 成功调用回包场景，确认返回对象包含固定 name 与原始 data 引用。</para>
        /// <para>en-us:Tests successful ClientA call payload to ensure returned object contains fixed name and original data reference.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：用桩客户端返回预设对象，调用 ClientA 后通过反射读取匿名对象字段并断言值正确。</para>
        /// <para>en-us:Process: return a preset object from stub client, call ClientA, read anonymous fields via reflection, and assert values.</para>
        /// </remarks>
        [Fact]
        public async Task ClientA_should_return_success_payload_when_taxin_call_succeeds()
        {
            const string expectedRoute = "taxin.service.test";
            var expectedPayload = new { result = "ok" };
            var client = new StubTaxinClient
            {
                Handler = (route, _, _) =>
                {
                    Assert.Equal(expectedRoute, route);
                    return Task.FromResult<object>(expectedPayload);
                }
            };

            var service = new TaxinConnectService(client);

            var result = await service.ClientA();
            var name = ReadProperty<string>(result, "name");
            var data = ReadProperty<object>(result, "data");

            Assert.Equal("132", name);
            Assert.Same(expectedPayload, data);
        }

        /// <summary>
        /// <para>zh-cn:测试 ClientA 异常兜底场景，确认远端异常会转换为返回数据中的错误消息。</para>
        /// <para>en-us:Tests ClientA failure fallback to ensure remote exception is converted into returned payload message.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：让桩客户端抛出 InvalidOperationException，调用 ClientA 后断言 name 固定且 data 为异常消息。</para>
        /// <para>en-us:Process: force stub client to throw InvalidOperationException, call ClientA, and assert fixed name plus exception message in data.</para>
        /// </remarks>
        [Fact]
        public async Task ClientA_should_return_exception_message_when_taxin_call_fails()
        {
            const string expectedMessage = "taxin-failed";
            var client = new StubTaxinClient
            {
                Handler = (_, _, _) => Task.FromException<object>(new InvalidOperationException(expectedMessage))
            };

            var service = new TaxinConnectService(client);

            var result = await service.ClientA();
            var name = ReadProperty<string>(result, "name");
            var data = ReadProperty<object>(result, "data");

            Assert.Equal("132", name);
            Assert.Equal(expectedMessage, data?.ToString());
        }

        /// <summary>
        /// <para>zh-cn:测试 ClientB 固定服务名输出场景，确认输入响应模型后返回对象的 name 为约定值。</para>
        /// <para>en-us:Tests fixed service-name output of ClientB to ensure returned name matches the expected constant.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造 TaxinResponseModel 调用 ClientB，读取匿名对象 name 字段并断言为 TaxinServiceTest。</para>
        /// <para>en-us:Process: build TaxinResponseModel, call ClientB, read anonymous name field, and assert it equals TaxinServiceTest.</para>
        /// </remarks>
        [Fact]
        public void ClientB_should_return_expected_service_name_payload()
        {
            var service = new TaxinConnectService();
            var responseModel = new TaxinResponseModel
            {
                name = "input",
                Name = "InputName",
                Description = new List<string> { "desc-1" },
                Routes = new List<RouteValueDictionary>()
            };

            var result = service.ClientB(responseModel);
            var name = ReadProperty<string>(result, "name");

            Assert.Equal("TaxinServiceTest", name);
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
        /// <para>zh-cn:用于 Taxin 客户端调用测试的最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation used by the Taxin client invocation tests.</para>
        /// </summary>
        private sealed class StubTaxinClient : ITaxinClientStandard
        {
            /// <summary>
            /// <para>zh-cn:处理 SendAsync 调用的委托。</para>
            /// <para>en-us:Delegate used to handle SendAsync invocations.</para>
            /// </summary>
            public Func<string, object?, Tuple<Version, Version>?, Task<object>> Handler { get; set; } = (_, _, _) => Task.FromResult<object>(new object());

            /// <summary>
            /// <para>zh-cn:发送远程调用请求。</para>
            /// <para>en-us:Sends a remote invocation request.</para>
            /// </summary>
            /// <typeparam name="TResult">
            /// <para>zh-cn:返回结果类型。</para>
            /// <para>en-us:The expected result type.</para>
            /// </typeparam>
            /// <param name="Route">
            /// <para>zh-cn:请求路由标识。</para>
            /// <para>en-us:The route key of the request.</para>
            /// </param>
            /// <param name="Data">
            /// <para>zh-cn:请求携带的数据。</para>
            /// <para>en-us:The payload sent with the request.</para>
            /// </param>
            /// <param name="Version">
            /// <para>zh-cn:请求使用的版本范围。</para>
            /// <para>en-us:The version range used for the request.</para>
            /// </param>
            /// <returns>
            /// <para>zh-cn:返回远程调用结果。</para>
            /// <para>en-us:Returns the remote invocation result.</para>
            /// </returns>
            public async Task<TResult> SendAsync<TResult>(string Route, object Data = null!, Tuple<Version, Version> Version = null!) where TResult : class
            {
                var result = await Handler(Route, Data, Version);
                return result as TResult
                    ?? throw new InvalidCastException($"The handler result cannot be converted to {typeof(TResult).FullName}.");
            }

            /// <summary>
            /// <para>zh-cn:空实现推送动作，仅用于满足测试桩接口契约。</para>
            /// <para>en-us:No-op push implementation to satisfy the test stub interface contract.</para>
            /// </summary>
            public Task PushAsync() => Task.CompletedTask;

            /// <summary>
            /// <para>zh-cn:空实现拉取动作，仅用于满足测试桩接口契约。</para>
            /// <para>en-us:No-op pull implementation to satisfy the test stub interface contract.</para>
            /// </summary>
            public Task PullAsync() => Task.CompletedTask;

            /// <summary>
            /// <para>zh-cn:空实现检查动作，仅用于满足测试桩接口契约。</para>
            /// <para>en-us:No-op check implementation to satisfy the test stub interface contract.</para>
            /// </summary>
            public Task CheckAsync() => Task.CompletedTask;

            /// <summary>
            /// <para>zh-cn:空实现上线动作，仅用于满足测试桩接口契约。</para>
            /// <para>en-us:No-op online implementation to satisfy the test stub interface contract.</para>
            /// </summary>
            public Task OnLineAsync() => Task.CompletedTask;

            /// <summary>
            /// <para>zh-cn:空实现下线动作，仅用于满足测试桩接口契约。</para>
            /// <para>en-us:No-op offline implementation to satisfy the test stub interface contract.</para>
            /// </summary>
            public Task OffLineAsync() => Task.CompletedTask;
        }
    }
}
