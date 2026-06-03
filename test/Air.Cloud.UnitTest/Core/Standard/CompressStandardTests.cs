using Air.Cloud.Core.Standard.Compress.Defaults;

using System.Text;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:压缩标准默认实现的单元测试，覆盖字符串、字节数组和流的基础闭环。</para>
    /// <para>en-us:Unit tests for the default compression standard, covering string, byte-array, and stream round trips.</para>
    /// </summary>
    public class CompressStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证字符串压缩后可以无损解压，确保业务消息、配置片段等文本内容不会被破坏。</para>
        /// <para>en-us:Verifies string compression can be decompressed losslessly so business messages and configuration fragments are preserved.</para>
        /// </summary>
        [Fact]
        public void Compress_should_roundtrip_string_content()
        {
            var standard = new DefaultCompressStandardDependency();
            const string content = "Air.Cloud 标准压缩测试 / standard compression test.";

            var compressed = standard.Compress(content);
            var decompressed = standard.Decompress(compressed);

            Assert.NotEqual(content, compressed);
            Assert.Equal(content, decompressed);
        }

        /// <summary>
        /// <para>zh-cn:验证空字符串保持空字符串，避免调用方在无内容场景下额外处理异常分支。</para>
        /// <para>en-us:Verifies empty strings remain empty so callers do not need extra exceptional handling for no-content scenarios.</para>
        /// </summary>
        [Fact]
        public void Compress_should_return_empty_when_string_is_empty()
        {
            var standard = new DefaultCompressStandardDependency();

            Assert.Equal(string.Empty, standard.Compress(string.Empty));
            Assert.Equal(string.Empty, standard.Decompress(string.Empty));
        }

        /// <summary>
        /// <para>zh-cn:验证字节数组压缩闭环，确保非字符串调用路径和字符串调用路径行为一致。</para>
        /// <para>en-us:Verifies byte-array compression round trip so the non-string path remains consistent with the string path.</para>
        /// </summary>
        [Fact]
        public void Compress_should_roundtrip_byte_array()
        {
            var standard = new DefaultCompressStandardDependency();
            var content = Encoding.UTF8.GetBytes("byte-array-payload-字节内容");

            var compressed = standard.Compress(content);
            var decompressed = standard.Decompress(compressed);

            Assert.NotEqual(content, compressed);
            Assert.Equal(content, decompressed);
        }

        /// <summary>
        /// <para>zh-cn:验证空流和空字节数组沿用当前默认实现的空值约定。</para>
        /// <para>en-us:Verifies empty stream and null byte-array behavior follows the current default implementation contract.</para>
        /// </summary>
        [Fact]
        public void Compress_should_keep_null_contract_for_empty_stream_and_null_buffer()
        {
            var standard = new DefaultCompressStandardDependency();

            Assert.Null(standard.Compress((byte[])null!));
            Assert.Null(standard.Decompress((byte[])null!));
            Assert.Null(standard.Compress(new MemoryStream()));
            Assert.Null(standard.Decompress(new MemoryStream()));
        }

        /// <summary>
        /// <para>zh-cn:验证流转字节数组会从流起始位置读取全部内容，避免调用方当前位置影响结果。</para>
        /// <para>en-us:Verifies stream-to-byte conversion reads from the beginning so the caller's current position does not affect the result.</para>
        /// </summary>
        [Fact]
        public void StreamToBytes_should_read_stream_from_beginning()
        {
            var standard = new DefaultCompressStandardDependency();
            var content = Encoding.UTF8.GetBytes("stream-payload");
            using var stream = new MemoryStream(content);
            stream.Seek(6, SeekOrigin.Begin);

            var bytes = standard.StreamToBytes(stream);

            Assert.Equal(content, bytes);
        }
    }
}
