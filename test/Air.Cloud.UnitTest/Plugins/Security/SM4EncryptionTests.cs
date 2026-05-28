using Air.Cloud.Core.Plugins.Security.SM4;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class SM4EncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 SM4 ECB 模式闭环场景，确认 ECB 加解密后可恢复原始内容。</para>
        /// <para>en-us:Tests SM4 ECB round-trip to ensure content is restored after ECB encrypt/decrypt.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：使用固定 key 对内容执行 ECB 加密再解密，断言明文恢复一致。</para>
        /// <para>en-us:Process: use fixed key for ECB encrypt/decrypt and assert plaintext restoration.</para>
        /// </remarks>
        [Fact]
        public void EncryptECB_and_decryptECB_should_roundtrip()
        {
            const string content = "SM4 ECB payload";
            const string key = "1234567890abcdef";

            var cipher = SM4Encryption.EncryptECB(content, key);
            var restored = SM4Encryption.DecryptECB(cipher, key);

            Assert.Equal(content, restored);
        }

        /// <summary>
        /// <para>zh-cn:测试 SM4 CBC 模式闭环场景，确认在固定 key+iv 下 CBC 可正确还原明文。</para>
        /// <para>en-us:Tests SM4 CBC round-trip to ensure plaintext restoration with fixed key and IV.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：使用固定 key 与 iv 执行 CBC 加解密，断言输出与输入明文一致。</para>
        /// <para>en-us:Process: perform CBC encrypt/decrypt using fixed key and IV, then assert output equals input plaintext.</para>
        /// </remarks>
        [Fact]
        public void EncryptCBC_and_decryptCBC_should_roundtrip()
        {
            const string content = "SM4 CBC payload";
            const string key = "1234567890abcdef";
            const string iv = "abcdef1234567890";

            var cipher = SM4Encryption.EncryptCBC(content, key, iv);
            var restored = SM4Encryption.DecryptCBC(cipher, key, iv);

            Assert.Equal(content, restored);
        }
    }
}
