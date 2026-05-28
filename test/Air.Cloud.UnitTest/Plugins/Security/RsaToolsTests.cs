using Air.Cloud.Core.Plugins.Security.RSA;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class RsaToolsTests
    {
        /// <summary>
        /// <para>zh-cn:测试 RSA 密钥生成场景，确认工具方法可返回非空公钥与私钥。</para>
        /// <para>en-us:Tests RSA key generation to ensure utility returns non-empty public and private keys.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：调用 CreateRSAKey 获取结果并断言两段密钥字符串均非空白。</para>
        /// <para>en-us:Process: call CreateRSAKey and assert both key strings are not null/whitespace.</para>
        /// </remarks>
        [Fact]
        public void CreateRSAKey_should_return_public_and_private_key()
        {
            var keys = RsaTools.CreateRSAKey();

            Assert.False(string.IsNullOrWhiteSpace(keys.PublicKey));
            Assert.False(string.IsNullOrWhiteSpace(keys.PrivateKey));
        }
    }
}
