using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:输出标准与追踪日志内容默认实现的单元测试。</para>
    /// <para>en-us:Unit tests for the output standard and default trace-log content.</para>
    /// </summary>
    public class PrintAndTraceLogStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证普通输出被格式化为单行，附加参数会以内联 Params 形式输出。</para>
        /// <para>en-us:Verifies information output is formatted as one line and additional parameters are written inline as Params.</para>
        /// </summary>
        [Fact]
        public void Print_should_write_single_line_information_with_inline_params()
        {
            var output = new DefaultAppOutputDependency();
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                output.Print(new AppPrintInformation
                {
                    Title = "standard-title",
                    Content = "line1\r\nline2",
                    Level = AppPrintLevel.Information,
                    SourceAssembly = "Air.Cloud.UnitTest",
                    AdditionalParams = new Dictionary<string, object>
                    {
                        ["traceId"] = "T-001"
                    }
                });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var result = writer.ToString();
            Assert.Contains("[info]", result);
            Assert.Contains("[Air.Cloud.UnitTest]", result);
            Assert.Contains("standard-title | line1 line2", result);
            Assert.Contains("Params:{traceId=T-001}", result);
        }

        /// <summary>
        /// <para>zh-cn:验证错误输出会把附加参数展开为多行异常块，便于排查堆栈、上下文等长文本。</para>
        /// <para>en-us:Verifies error output expands additional parameters as a multiline exception block for stack traces and context text.</para>
        /// </summary>
        [Fact]
        public void Print_should_write_multiline_error_params()
        {
            var output = new DefaultAppOutputDependency();
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                output.Print(new AppPrintInformation
                {
                    Title = "error-title",
                    Content = "error-content",
                    Level = AppPrintLevel.Error,
                    SourceAssembly = "Air.Cloud.UnitTest",
                    AdditionalParams = new Dictionary<string, object>
                    {
                        ["stack"] = "line1\nline2"
                    }
                });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var result = writer.ToString();
            Assert.Contains("[error]", result);
            Assert.Contains("Exception:", result);
            Assert.Contains("stack:", result);
            Assert.Contains("line1", result);
            Assert.Contains("line2", result);
        }

        /// <summary>
        /// <para>zh-cn:验证多行文本判断对 Windows 与 Unix 换行符都有效。</para>
        /// <para>en-us:Verifies multiline detection works for both Windows and Unix line endings.</para>
        /// </summary>
        [Theory]
        [InlineData("one-line", false, 1)]
        [InlineData("line1\nline2", true, 2)]
        [InlineData("line1\r\nline2", true, 2)]
        public void IsMultilineText_should_detect_line_count(string text, bool expected, int expectedLineCount)
        {
            var isMultiline = DefaultAppOutputDependency.IsMultilineText(text, out var lineCount);

            Assert.Equal(expected, isMultiline);
            Assert.Equal(expectedLineCount, lineCount);
        }

        /// <summary>
        /// <para>zh-cn:验证默认追踪日志内容会生成标识、保留标题内容并按传入标签拼接。</para>
        /// <para>en-us:Verifies default trace-log content creates an id, keeps title/content, and joins provided tags.</para>
        /// </summary>
        [Fact]
        public void DefaultTraceLogContent_should_initialize_values()
        {
            var additionalParams = new Dictionary<string, object> { ["key"] = "value" };

            var content = new DefaultTraceLogContent("trace-title", "trace-content", additionalParams, "a", "b");

            Assert.False(string.IsNullOrWhiteSpace(content.Id));
            Assert.Equal("a,b", content.Tags);
            Assert.Equal("trace-title", content.Title);
            Assert.Equal("trace-content", content.Content);
            Assert.Same(additionalParams, content.AdditionalParams);
        }
    }
}
