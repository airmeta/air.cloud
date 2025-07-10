using Air.Cloud.Core.App;

namespace Air.Cloud.UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void TheEnvironmentIsDevelopment()
        {
            //调试情况下为true 运行情况下为false
            Assert.False(AppEnvironment.IsDevelopment);
        }

        [Fact]
        /// <summary>
        /// <para>zh-cn:尝试加载环境变量值 </para> 
        /// <para>en-us:Try to load environment variable value</para>
        /// </summary>
        public void TryLoadEnvironmentKey()
        {
            Assert.Equal("Development", AppEnvironment.EnvironmentKey);
        }

        [Fact]
        /// <summary>
        /// <para>zh-cn:尝试加载环境变量值 </para> 
        /// <para>en-us:Try to load environment variable value</para>
        /// </summary>
        public void TryLoadVirtualEnvironmentStatus() 
        {
            Assert.Equal(Core.Enums.EnvironmentEnums.Test,AppEnvironment.VirtualEnvironment);
        }

        [Fact]
        /// <summary>
        /// <para>zh-cn:尝试加载环境变量值 </para> 
        /// <para>en-us:Try to load environment variable value</para>
        /// </summary>
        public void TryLoadRealEnvironmentStatus()
        {
            Assert.Equal(Core.Enums.EnvironmentEnums.Test, AppEnvironment.RealEnvironment);
        }

    }
}