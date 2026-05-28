using Air.Cloud.Core.Plugins.Security.MD5;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class MD5EncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 MD5 32 位摘要格式场景，确认大小写选项与输出长度符合约定。</para>
        /// <para>en-us:Tests 32-char MD5 format behavior, ensuring case option and output length follow expectations.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：分别获取 lower/upper 版本摘要，断言长度为 32 且 upper 等于 lower 的大写形式。</para>
        /// <para>en-us:Process: get lower and upper digests, assert 32 length, and verify upper equals uppercase lower.</para>
        /// </remarks>
        [Fact]
        public void GetMd5By32_should_return_expected_length_and_case()
        {
            var lower = MD5Encryption.GetMd5By32("md5-content", false);
            var upper = MD5Encryption.GetMd5By32("md5-content", true);

            Assert.Equal(32, lower.Length);
            Assert.Equal(32, upper.Length);
            Assert.Equal(lower.ToUpperInvariant(), upper);
        }

        /// <summary>
        /// <para>zh-cn:测试 MD5 截断摘要长度场景，确认 16/8/4 位接口返回长度准确。</para>
        /// <para>en-us:Tests truncated MD5 lengths to ensure 16/8/4 helper methods return exact expected sizes.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：对同一输入分别调用 16/8/4 位方法并断言各自字符串长度。</para>
        /// <para>en-us:Process: call 16/8/4 variants with the same input and assert each returned string length.</para>
        /// </remarks>
        [Fact]
        public void GetMd5By16_8_4_should_return_expected_lengths()
        {
            var hash16 = MD5Encryption.GetMd5By16("md5-content");
            var hash8 = MD5Encryption.GetMd5By8("md5-content");
            var hash4 = MD5Encryption.GetMd5By4("md5-content");

            Assert.Equal(16, hash16.Length);
            Assert.Equal(8, hash8.Length);
            Assert.Equal(4, hash4.Length);
        }
    }
}
