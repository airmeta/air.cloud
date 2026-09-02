param(
    # zh-cn:发布输出根目录；该目录内容可直接挂载到 DynamicApp 容器的 /app。
    # en-us:Publish output root; its contents can be mounted directly to /app in the DynamicApp container.
    [string]$OutputRoot = "X:\air.cloud.tmp\dynamicapp-outofprocess",

    # zh-cn:发布配置，默认 Release。
    # en-us:Publish configuration, Release by default.
    [string]$Configuration = "Release",

    # zh-cn:发布前清空输出目录。
    # en-us:Clean the output directory before publishing.
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$dynamicAppRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $dynamicAppRoot "..\..\..")
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

if ($Clean -and (Test-Path -LiteralPath $resolvedOutputRoot)) {
    if ([string]::IsNullOrWhiteSpace($resolvedOutputRoot) -or $resolvedOutputRoot.Length -lt 10) {
        throw "Refusing to clean an unsafe output path: $resolvedOutputRoot"
    }

    Remove-Item -LiteralPath $resolvedOutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $resolvedOutputRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutputRoot "mods") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutputRoot "manifests") | Out-Null

function Publish-AirCloudProject {
    param(
        # zh-cn:相对于仓库根目录的项目路径。
        # en-us:Project path relative to the repository root.
        [Parameter(Mandatory = $true)]
        [string]$Project,

        # zh-cn:相对于输出根目录的发布子目录。
        # en-us:Publish subdirectory relative to the output root.
        [Parameter(Mandatory = $true)]
        [string]$Target
    )

    $projectPath = Join-Path $repoRoot $Project
    $targetPath = Join-Path $resolvedOutputRoot $Target
    New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
    dotnet publish $projectPath -c $Configuration -o $targetPath --nologo
}

function Set-ProcessModServiceAddress {
    param(
        # zh-cn:相对于输出根目录的 Mod 发布子目录。
        # en-us:Mod publish subdirectory relative to the output root.
        [Parameter(Mandatory = $true)]
        [string]$Target,

        # zh-cn:子进程向 Consul 注册的容器网络地址。
        # en-us:Container-network address registered by the child process in Consul.
        [Parameter(Mandatory = $true)]
        [string]$ServiceAddress
    )

    $appsettingsPath = Join-Path (Join-Path $resolvedOutputRoot $Target) "appsettings.json"
    if (!(Test-Path -LiteralPath $appsettingsPath)) {
        throw "Cannot find appsettings.json for Process Mod: $appsettingsPath"
    }

    $json = Get-Content -LiteralPath $appsettingsPath -Raw | ConvertFrom-Json
    if ($null -eq $json.ConsulServiceOptions) {
        throw "Cannot find ConsulServiceOptions in: $appsettingsPath"
    }

    $json.ConsulServiceOptions.ServiceAddress = $ServiceAddress
    $json.ConsulServiceOptions.DynamicAppManaged = $true
    $json.ConsulServiceOptions.EnableServiceRegistration = $false
    $json | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $appsettingsPath -Encoding UTF8
}

Publish-AirCloudProject `
    -Project "demo\api\DynamicApp\air.cloud.dynamicapp.entry\air.cloud.dynamicapp.entry.csproj" `
    -Target "."

Publish-AirCloudProject `
    -Project "demo\api\Security\Auth\air.cloud.auth.entry\air.cloud.auth.entry.csproj" `
    -Target "mods\auth"

Set-ProcessModServiceAddress `
    -Target "mods\auth" `
    -ServiceAddress "http://air.dynamicapp:5106"

Publish-AirCloudProject `
    -Project "demo\api\Security\Account\air.cloud.account.entry\air.cloud.account.entry.csproj" `
    -Target "mods\account"

Set-ProcessModServiceAddress `
    -Target "mods\account" `
    -ServiceAddress "http://air.dynamicapp:5108"

Publish-AirCloudProject `
    -Project "demo\api\Security\System\air.cloud.system.entry\air.cloud.system.entry.csproj" `
    -Target "mods\system"

Set-ProcessModServiceAddress `
    -Target "mods\system" `
    -ServiceAddress "http://air.dynamicapp:5107"

Copy-Item `
    -LiteralPath (Join-Path $dynamicAppRoot "manifests\security-platform.process.101.json") `
    -Destination (Join-Path $resolvedOutputRoot "manifests\security-platform.process.101.json") `
    -Force

Write-Host "DynamicApp OutOfProcess publish completed: $resolvedOutputRoot"
Write-Host "Container mount target: /app"
Write-Host "Manifest: /app/manifests/security-platform.process.101.json"
