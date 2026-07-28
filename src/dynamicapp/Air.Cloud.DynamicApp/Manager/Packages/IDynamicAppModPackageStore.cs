namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包存储接口，负责保存上传压缩包并解析出可启动工件位置。</para>
/// <para>en-us:DynamicApp Mod package store interface responsible for saving uploaded archives and resolving startable artifact locations.</para>
/// </summary>
public interface IDynamicAppModPackageStore
{
    /// <summary>
    /// <para>zh-cn:保存并解压一个 Mod 压缩包；实现方必须校验路径边界、防止压缩包逃逸写入，并在入口文件缺省时推断入口 .dll。</para>
    /// <para>en-us:Saves and extracts a Mod archive; implementations must validate path boundaries, prevent archive extraction escape, and infer the entry .dll when the entry file is omitted.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:包保存请求，包含目标键、可选入口文件和覆盖策略。</para>
    /// <para>en-us:The package save request containing the target key, optional entry file, and overwrite policy.</para>
    /// </param>
    /// <param name="packageStream">
    /// <para>zh-cn:上传压缩包数据流，调用方负责打开和关闭该流。</para>
    /// <para>en-us:The uploaded archive stream; callers own stream opening and disposal.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包保存记录，包含解压目录和入口文件绝对路径。</para>
    /// <para>en-us:The package save record containing the extraction directory and absolute entry file path.</para>
    /// </returns>
    Task<DynamicAppModPackageRecord> SaveAsync(DynamicAppModPackageSaveRequest request, Stream packageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:列出当前包存储根目录下已经保存的所有 Mod 包版本，供控制面板包管理器查验和上传后刷新使用。</para>
    /// <para>en-us:Lists all Mod package versions currently saved under the package-store root for package-manager inspection and post-upload refresh.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按 DynamicApp 和 Mod 聚合后的包清单；包根目录不存在时返回空集合。</para>
    /// <para>en-us:Package inventory grouped by DynamicApp and Mod; an empty collection is returned when the package root does not exist.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppModPackageModView> ListPackages();

    /// <summary>
    /// <para>zh-cn:创建一个包管理应用目录，使操作员可以先选择应用再继续上传该应用下的 Mod 包。</para>
    /// <para>en-us:Creates a package-management application directory so operators can select the application before uploading Mod packages under it.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识；实现必须把它当作路径片段验证，避免写出包存储根目录。</para>
    /// <para>en-us:DynamicApp application identifier; implementations must validate it as a path segment to prevent writes outside the package-store root.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:创建或已存在的应用视图。</para>
    /// <para>en-us:The created or already existing application view.</para>
    /// </returns>
    DynamicAppPackageApplicationView CreateApplication(string dynamicAppId);

    /// <summary>
    /// <para>zh-cn:列出包存储中已创建的应用目录，包含暂未上传 Mod 包的空应用。</para>
    /// <para>en-us:Lists application directories already created in the package store, including empty applications without uploaded Mod packages.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:包存储应用集合；目录不存在时返回空集合。</para>
    /// <para>en-us:Package-store application collection; an empty collection is returned when the directory does not exist.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppPackageApplicationView> ListApplications();
}
