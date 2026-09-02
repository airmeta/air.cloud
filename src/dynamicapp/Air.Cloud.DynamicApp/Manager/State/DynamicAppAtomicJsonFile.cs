using System.Text;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp JSON 文件原子写入帮助类，确保状态文件替换期间不会留下半截 JSON。</para>
/// <para>en-us:Atomic JSON file writer for DynamicApp, ensuring state replacement does not leave a partially written JSON document.</para>
/// </summary>
internal static class DynamicAppAtomicJsonFile
{
    /// <summary>
    /// <para>zh-cn:将 JSON 内容写入同目录临时文件并以同卷替换目标文件。</para>
    /// <para>en-us:Writes JSON content to a temporary file in the same directory and replaces the target on the same volume.</para>
    /// </summary>
    /// <param name="filePath">
    /// <para>zh-cn:目标 JSON 文件路径。</para>
    /// <para>en-us:The target JSON file path.</para>
    /// </param>
    /// <param name="json">
    /// <para>zh-cn:待写入的 JSON 文本。</para>
    /// <para>en-us:The JSON text to write.</para>
    /// </param>
    public static void Write(string filePath, string json)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
