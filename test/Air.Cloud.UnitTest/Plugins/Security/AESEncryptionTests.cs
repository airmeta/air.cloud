using Air.Cloud.Core.Plugins.Security.AES;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class AESEncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 AES 显式密钥与向量加解密闭环场景，确认明文可完整还原。</para>
        /// <para>en-us:Tests AES round-trip with explicit key and IV to ensure plaintext can be fully restored.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：使用固定 key/iv 加密字符串，再解密并断言结果与原始明文一致。</para>
        /// <para>en-us:Process: encrypt text with fixed key/IV, decrypt it, and assert equality with original plaintext.</para>
        /// </remarks>
        [Fact]
        public void Encrypt_and_decrypt_should_roundtrip_with_explicit_key_and_iv()
        {
            const string plain = "AES unit test payload";
            const string key = "1234567890abcdef";
            const string iv = "abcdef1234567890";

            var cipher = AESEncryption.Encrypt(plain, key, iv);
            var restored = AESEncryption.Decrypt(cipher, key, iv);

            Assert.Equal(plain, restored);
        }
    }
}
