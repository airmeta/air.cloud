using Air.Cloud.Core.Standard.JSON.Converters;
using Air.Cloud.Core.Standard.JSON.Defaults;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:JSON 标准默认实现和系统转换器的单元测试。</para>
    /// <para>en-us:Unit tests for the default JSON standard and framework converters.</para>
    /// </summary>
    public class JsonStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证默认 JSON 标准可以完成对象序列化与反序列化闭环。</para>
        /// <para>en-us:Verifies the default JSON standard can serialize and deserialize an object round trip.</para>
        /// </summary>
        [Fact]
        public void DefaultJsonSerializer_should_roundtrip_object()
        {
            var standard = new DefaultJsonSerializerStandardDependency();
            var value = new JsonPayload
            {
                Id = 7,
                Name = "standard-json"
            };

            var json = standard.Serialize(value);
            var result = standard.Deserialize<JsonPayload>(json);

            Assert.Contains("standard-json", json);
            Assert.NotNull(result);
            Assert.Equal(value.Id, result.Id);
            Assert.Equal(value.Name, result.Name);
        }

        /// <summary>
        /// <para>zh-cn:验证 DateTime 转换器使用默认格式写出时间，保证 API 输出时间格式稳定。</para>
        /// <para>en-us:Verifies the DateTime converter writes the default format so API time output remains stable.</para>
        /// </summary>
        [Fact]
        public void DateTimeJsonConverter_should_write_default_format()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DateTimeJsonConverter());
            var value = new DateTime(2026, 6, 3, 12, 34, 56);

            var json = JsonSerializer.Serialize(value, options);
            var result = JsonSerializer.Deserialize<DateTime>(json, options);

            Assert.Equal("\"2026-06-03 12:34:56\"", json);
            Assert.Equal(value, result);
        }

        /// <summary>
        /// <para>zh-cn:验证异常转换器只输出可序列化属性，并且不会输出 TargetSite。</para>
        /// <para>en-us:Verifies the exception converter writes serializable properties and excludes TargetSite.</para>
        /// </summary>
        [Fact]
        public void ExceptionJsonConverter_should_write_exception_without_target_site()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new ExceptionJsonConverter<InvalidOperationException>());

            var json = JsonSerializer.Serialize(new InvalidOperationException("json-error"), options);

            Assert.Contains("json-error", json);
            Assert.Contains(nameof(Exception.Message), json);
            Assert.DoesNotContain(nameof(Exception.TargetSite), json);
        }

        /// <summary>
        /// <para>zh-cn:验证异常反序列化被明确禁止，避免从 JSON 重建异常对象带来兼容性和安全问题。</para>
        /// <para>en-us:Verifies exception deserialization is explicitly blocked to avoid compatibility and security issues.</para>
        /// </summary>
        [Fact]
        public void ExceptionJsonConverter_should_reject_deserialization()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ExceptionJsonConverter<InvalidOperationException>());

            Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Deserialize<InvalidOperationException>("{\"Message\":\"blocked\"}", options));
        }

        private sealed class JsonPayload
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }
    }
}
