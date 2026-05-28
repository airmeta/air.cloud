using Air.Cloud.Core.Plugins.Security.HmacSHA256;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class HmacSHA256HelperTests
    {
        /// <summary>
        /// <para>zh-cn:测试 HMAC-SHA256 稳定性场景，同一输入与密钥应生成一致摘要。</para>
        /// <para>en-us:Tests HMAC-SHA256 stability where identical input and key must produce the same digest.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：对相同 content/key 连续计算两次摘要，断言结果一致且非空白字符串。</para>
        /// <para>en-us:Process: compute digest twice with same content/key, then assert equality and non-empty output.</para>
        /// </remarks>
        [Fact]
        public void HmacSHA256Encrypt_should_return_stable_result_for_same_input()
        {
            const string content = "hmac-content";
            const string key = "hmac-key";

            var hash1 = HmacSHA256Helper.HmacSHA256Encrypt(content, key);
            var hash2 = HmacSHA256Helper.HmacSHA256Encrypt(content, key);

            Assert.Equal(hash1, hash2);
            Assert.False(string.IsNullOrWhiteSpace(hash1));
        }
    }
}
