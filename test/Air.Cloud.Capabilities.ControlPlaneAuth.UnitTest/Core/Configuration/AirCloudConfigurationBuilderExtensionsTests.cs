using Air.Cloud.Core.Extensions;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.UnitTest.Core.Configuration;

/// <summary>
/// <para>zh-cn:Air.Cloud 配置目录加载单元测试。</para>
/// <para>en-us:Unit tests for Air.Cloud configuration-directory loading.</para>
/// </summary>
public sealed class AirCloudConfigurationBuilderExtensionsTests
{
    /// <summary>
    /// <para>zh-cn:验证配置目录文件按名称升序加载，环境子目录在基础目录之后加载并覆盖同名配置键。</para>
    /// <para>en-us:Verifies that configuration-directory files are loaded by ascending file name and the environment subdirectory is loaded after the base directory to override duplicate keys.</para>
    /// </summary>
    [Fact]
    public void AddAirCloudConfigDirectory_should_load_sorted_files_and_environment_overrides()
    {
        var directory = CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(directory, "Development"));
        File.WriteAllText(Path.Combine(directory, "20-feature.json"), """{"Feature":{"Value":"base-20","Other":"from-20"}}""");
        File.WriteAllText(Path.Combine(directory, "10-feature.json"), """{"Feature":{"Value":"base-10"}}""");
        File.WriteAllText(Path.Combine(directory, "Development", "10-feature.json"), """{"Feature":{"Value":"development"}}""");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddAirCloudConfigDirectory(directory, "Development", optional: false, reloadOnChange: false)
                .Build();

            Assert.Equal("development", configuration["Feature:Value"]);
            Assert.Equal("from-20", configuration["Feature:Other"]);
            Assert.Equal(
                [
                    Path.Combine(directory, "10-feature.json"),
                    Path.Combine(directory, "20-feature.json"),
                    Path.Combine(directory, "Development", "10-feature.json")
                ],
                AirCloudConfigurationBuilderExtensions.GetAirCloudConfigDirectoryFiles(directory, "Development"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证必需配置目录缺失时会明确抛出异常，避免宿主误以为能力配置已经加载。</para>
    /// <para>en-us:Verifies that a missing required configuration directory throws a clear exception so the host does not assume capability configuration has been loaded.</para>
    /// </summary>
    [Fact]
    public void AddAirCloudConfigDirectory_should_throw_when_required_directory_is_missing()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => new ConfigurationBuilder().AddAirCloudConfigDirectory(missing, optional: false));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
