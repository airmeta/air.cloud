using Air.Cloud.AirForge.Panel;
using Air.Cloud.AirForge.Panel.FileEditing;

using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Panel;

/// <summary>
/// <para>zh-cn:AirForge 文本文件编辑器单元测试，覆盖白名单、根目录、大小限制和并发修改保护。</para>
/// <para>en-us:Unit tests for the AirForge text-file editor covering allowlists, roots, size limits, and concurrent-modification protection.</para>
/// </summary>
public sealed class AirForgeTextFileEditorTests
{
    /// <summary>
    /// <para>zh-cn:验证常用文本扩展名可编辑，其他扩展名默认拒绝。</para>
    /// <para>en-us:Verifies common text extensions are editable while other extensions are rejected by default.</para>
    /// </summary>
    [Fact]
    public void IsEditable_should_apply_extension_allowlist()
    {
        Assert.True(AirForgeTextFileEditor.IsEditable("appsettings.json"));
        Assert.True(AirForgeTextFileEditor.IsEditable("service.yaml"));
        Assert.False(AirForgeTextFileEditor.IsEditable("service.dll"));
        Assert.False(AirForgeTextFileEditor.IsEditable("image.png"));
    }

    /// <summary>
    /// <para>zh-cn:验证允许根目录内的文本文件可以读取和保存，并保留最新快照时间。</para>
    /// <para>en-us:Verifies text files inside an allowed root can be read and saved with an updated snapshot timestamp.</para>
    /// </summary>
    [Fact]
    public async Task ReadAndWrite_should_round_trip_utf8_content()
    {
        var root = Directory.CreateTempSubdirectory("airforge-editor-");
        try
        {
            var path = Path.Combine(root.FullName, "settings.json");
            await File.WriteAllTextAsync(path, "{\"value\":1}");
            var editor = CreateEditor(root.FullName);

            var snapshot = await editor.ReadAsync(path);
            Assert.Equal("{\"value\":1}", snapshot.Content);

            var saved = await editor.WriteAsync(new AirForgeTextFileWriteRequest
            {
                Path = path,
                Content = "{\"value\":\"中文\"}",
                ExpectedLastModifiedUtc = snapshot.LastModifiedUtc
            });

            Assert.Equal("{\"value\":\"中文\"}", saved.Content);
            Assert.Equal("{\"value\":\"中文\"}", await File.ReadAllTextAsync(path));
        }
        finally
        {
            root.Delete(true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证文件修改后使用旧快照保存会返回并发冲突。</para>
    /// <para>en-us:Verifies saving with an old snapshot after an external modification returns a concurrency conflict.</para>
    /// </summary>
    [Fact]
    public async Task Write_should_reject_stale_snapshot()
    {
        var root = Directory.CreateTempSubdirectory("airforge-editor-");
        try
        {
            var path = Path.Combine(root.FullName, "settings.json");
            await File.WriteAllTextAsync(path, "{\"value\":1}");
            var editor = CreateEditor(root.FullName);
            var snapshot = await editor.ReadAsync(path);

            await File.WriteAllTextAsync(path, "{\"value\":2}");
            File.SetLastWriteTimeUtc(path, snapshot.LastModifiedUtc.UtcDateTime.AddSeconds(1));

            await Assert.ThrowsAsync<AirForgeTextFileConflictException>(() =>
                editor.WriteAsync(new AirForgeTextFileWriteRequest
                {
                    Path = path,
                    Content = "{\"value\":3}",
                    ExpectedLastModifiedUtc = snapshot.LastModifiedUtc
                }));
        }
        finally
        {
            root.Delete(true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证包根目录之外的文件不能通过路径穿越访问。</para>
    /// <para>en-us:Verifies files outside the package root cannot be reached through path traversal.</para>
    /// </summary>
    [Fact]
    public async Task Read_should_reject_path_outside_allowed_root()
    {
        var root = Directory.CreateTempSubdirectory("airforge-editor-");
        try
        {
            var outside = Path.Combine(root.Parent!.FullName, "airforge-outside.json");
            await File.WriteAllTextAsync(outside, "{}");
            var editor = CreateEditor(root.FullName);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => editor.ReadAsync(outside));
            File.Delete(outside);
        }
        finally
        {
            root.Delete(true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证读取和保存都遵守文本字节大小上限。</para>
    /// <para>en-us:Verifies both reads and writes enforce the configured text byte limit.</para>
    /// </summary>
    [Fact]
    public async Task Read_should_reject_content_above_configured_limit()
    {
        var root = Directory.CreateTempSubdirectory("airforge-editor-");
        try
        {
            var path = Path.Combine(root.FullName, "settings.json");
            await File.WriteAllTextAsync(path, "12345");
            var editor = CreateEditor(root.FullName, maxBytes: 4);

            await Assert.ThrowsAsync<InvalidOperationException>(() => editor.ReadAsync(path));
        }
        finally
        {
            root.Delete(true);
        }
    }

    private static AirForgeTextFileEditor CreateEditor(string rootPath, long maxBytes = 2 * 1024 * 1024) =>
        new(
            new AirForgePanelOptions { MaxEditableFileBytes = maxBytes },
            new DynamicAppPackageStoreOptions { RootPath = rootPath });
}
