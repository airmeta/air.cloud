using Air.Cloud.Core.Plugins.Security.SM3;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class SM3EncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 SM3 无密钥哈希稳定性，确认相同输入多次计算结果一致且长度为 64。</para>
        /// <para>en-us:Tests SM3 hash stability without key, ensuring repeated hashing of same input is identical with length 64.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：对同一内容连续计算两次哈希，比较结果一致并校验摘要长度。</para>
        /// <para>en-us:Process: hash the same content twice, compare equality, and validate digest length.</para>
        /// </remarks>
        [Fact]
        public void Entrypt_should_return_consistent_hash_for_same_content()
        {
            const string content = "SM3 payload";

            var hash1 = SM3Encryption.Entrypt(content);
            var hash2 = SM3Encryption.Entrypt(content);

            Assert.Equal(hash1, hash2);
            Assert.Equal(64, hash1.Length);
        }

        /// <summary>
        /// <para>zh-cn:测试 SM3 带密钥哈希稳定性，确认同 content+key 组合输出固定且长度正确。</para>
        /// <para>en-us:Tests keyed SM3 stability, ensuring same content+key pair yields stable digest with correct length.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：使用固定 content/key 计算两次摘要，断言结果一致且长度为 64。</para>
        /// <para>en-us:Process: compute keyed hash twice with fixed content/key, assert equality and 64-char length.</para>
        /// </remarks>
        [Fact]
        public void Entrypt_with_key_should_return_consistent_hash_for_same_content_and_key()
        {
            const string content = "SM3 payload";
            const string key = "sm3-secret";

            var hash1 = SM3Encryption.Entrypt(content, key);
            var hash2 = SM3Encryption.Entrypt(content, key);

            Assert.Equal(hash1, hash2);
            Assert.Equal(64, hash1.Length);
        }
    }
}
