namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:命令式 DynamicApp 编排器选项；外部命令只在显式启用、命令通过安全校验后执行。</para>
/// <para>en-us:Options for the command-based DynamicApp orchestrator; external commands run only when explicitly enabled and validated by safety checks.</para>
/// </summary>
public sealed class CommandDynamicAppOrchestratorOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用命令式编排器，默认 false。</para>
    /// <para>en-us:Whether the command-based orchestrator is enabled, false by default.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:受控命令文件名，例如 docker、kubectl 或内部包装脚本。</para>
    /// <para>en-us:Trusted command file name, such as docker, kubectl, or an internal wrapper script.</para>
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:命令文件白名单；设置后 FileName 的文件名部分必须命中该列表。</para>
    /// <para>en-us:Allowed command file names; when set, the file-name portion of FileName must match this list.</para>
    /// </summary>
    public string[] AllowedFileNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// <para>zh-cn:是否要求 FileName 是绝对路径，默认 false 以兼容 docker/kubectl 这类 PATH 命令。</para>
    /// <para>en-us:Whether FileName must be an absolute path, false by default to remain compatible with PATH commands such as docker or kubectl.</para>
    /// </summary>
    public bool RequireAbsoluteFileName { get; set; }

    /// <summary>
    /// <para>zh-cn:是否要求显式配置 WorkingDirectory，默认 false。</para>
    /// <para>en-us:Whether WorkingDirectory must be configured explicitly, false by default.</para>
    /// </summary>
    public bool RequireConfiguredWorkingDirectory { get; set; }

    /// <summary>
    /// <para>zh-cn:是否阻止通用 Shell 命令作为编排入口，默认 true。</para>
    /// <para>en-us:Whether common shell commands are blocked as orchestration entry points, true by default.</para>
    /// </summary>
    public bool BlockShellCommands { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:命令参数模板，支持 {dynamicAppId}、{modId}、{version}、{deploymentId}、{image} 和 {healthPath} 占位符。</para>
    /// <para>en-us:Command argument template supporting {dynamicAppId}, {modId}, {version}, {deploymentId}, {image}, and {healthPath} placeholders.</para>
    /// </summary>
    public string ArgumentsTemplate { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:停止命令参数模板，支持 {dynamicAppId}、{modId}、{version} 和 {deploymentId} 占位符；为空时 StopAsync 会拒绝执行。</para>
    /// <para>en-us:Stop command argument template supporting {dynamicAppId}, {modId}, {version}, and {deploymentId}; StopAsync rejects execution when it is empty.</para>
    /// </summary>
    public string StopArgumentsTemplate { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:命令工作目录；为空时使用当前进程工作目录，除非 RequireConfiguredWorkingDirectory 为 true。</para>
    /// <para>en-us:Command working directory; the current process working directory is used when empty unless RequireConfiguredWorkingDirectory is true.</para>
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// <para>zh-cn:命令执行超时时间，默认 120 秒。</para>
    /// <para>en-us:Command execution timeout, 120 seconds by default.</para>
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// <para>zh-cn:命令完成后发布到状态存储的目标基础地址模板；为空时读取 Artifact.Endpoint。</para>
    /// <para>en-us:Target base address template published to the state store after the command completes; Artifact.Endpoint is used when empty.</para>
    /// </summary>
    public string? TargetBaseAddressTemplate { get; set; }
}
