using Air.Cloud.Core.Standard.AppResult;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:统一返回标准契约的单元测试，确保 WebApp 统一返回可依赖稳定字段形状。</para>
    /// <para>en-us:Unit tests for unified-result standard contracts, ensuring WebApp unified responses can rely on stable field shape.</para>
    /// </summary>
    public class AppResultStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证带数据负载的 RESTful 结果标准可以承载成功响应的核心字段。</para>
        /// <para>en-us:Verifies the RESTful result standard with payload can carry the core fields of a successful response.</para>
        /// </summary>
        [Fact]
        public void RestfulResultStandard_with_payload_should_preserve_success_fields()
        {
            IRESTfulResultStandard<TestResultData> result = new TestRestfulResult<TestResultData>
            {
                Code = 200,
                Data = new TestResultData { Name = "created" },
                Succeeded = true,
                Message = "ok",
                Timestamp = 1780454400000,
                Extras = new { TraceId = "trace-1" }
            };

            Assert.Equal(200, result.Code);
            Assert.True(result.Succeeded);
            Assert.Equal("created", result.Data.Name);
            Assert.Equal("ok", result.Message);
            Assert.Equal(1780454400000, result.Timestamp);
            Assert.NotNull(result.Extras);
        }

        /// <summary>
        /// <para>zh-cn:验证不带数据负载的 RESTful 结果标准可以承载失败响应的错误信息。</para>
        /// <para>en-us:Verifies the RESTful result standard without payload can carry error information for failed responses.</para>
        /// </summary>
        [Fact]
        public void RestfulResultStandard_without_payload_should_preserve_error_fields()
        {
            IRESTfulResultStandard result = new TestRestfulResult
            {
                Code = 400,
                Succeeded = false,
                Errors = new[] { "Name is required" },
                Timestamp = 1780454400000
            };

            Assert.Equal(400, result.Code);
            Assert.False(result.Succeeded);
            Assert.NotNull(result.Errors);
            Assert.Equal(1780454400000, result.Timestamp);
        }

        private sealed class TestResultData
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class TestRestfulResult<T> : IRESTfulResultStandard<T>
        {
            public int? Code { get; set; }

            public T Data { get; set; } = default!;

            public bool Succeeded { get; set; }

            public object Errors { get; set; } = null!;

            public object Extras { get; set; } = null!;

            public long Timestamp { get; set; }

            public string Message { get; set; } = string.Empty;
        }

        private sealed class TestRestfulResult : IRESTfulResultStandard
        {
            public int? Code { get; set; }

            public bool Succeeded { get; set; }

            public object Errors { get; set; } = null!;

            public object Extras { get; set; } = null!;

            public long Timestamp { get; set; }
        }
    }
}
