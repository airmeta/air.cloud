param(
    [string]$AgentExportPackage = "",

    # The publish input is intentionally fixed to .\publish-input\win-x64.
    # Keep this optional alias for callers that still pass the old parameter;
    # a different path is rejected rather than silently used.
    [Alias("PublishDirectory", "PublishPackage")]
    [string]$PublishInput = "",

    # Output is intentionally fixed to .\publishs. This parameter is retained
    # only for source compatibility and is rejected when it points elsewhere.
    [string]$OutputDirectory = "",

    [string]$UserName = "",

    [string]$Version = "",

    [string]$NssmPath = "",

    [switch]$Force
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
}

function Expand-SafeZip {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipPath,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    $destinationRoot = [System.IO.Path]::GetFullPath($Destination)
    $destinationPrefix = $destinationRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    New-Item -ItemType Directory -Force -Path $destinationRoot | Out-Null

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.FullName)) {
                continue
            }

            $relativeName = $entry.FullName.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
            $targetPath = [System.IO.Path]::GetFullPath((Join-Path $destinationRoot $relativeName))
            if (-not $targetPath.StartsWith($destinationPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "ZIP entry escapes the extraction directory: $($entry.FullName)"
            }

            if ($entry.FullName.EndsWith("/", [System.StringComparison]::Ordinal)) {
                New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
                continue
            }

            $parent = Split-Path -Parent $targetPath
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-PublishRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InputDirectory
    )

    $directExecutable = Join-Path $InputDirectory "air.cloud.airtunnel.exe"
    if (Test-Path -LiteralPath $directExecutable -PathType Leaf) {
        return $InputDirectory
    }

    $matches = @(Get-ChildItem -LiteralPath $InputDirectory -Recurse -File -Filter "air.cloud.airtunnel.exe")
    if ($matches.Count -eq 1) {
        return $matches[0].Directory.FullName
    }

    if ($matches.Count -eq 0) {
        throw "固定发布输入目录不包含 air.cloud.airtunnel.exe，请把 win-x64 发布文件放入: $InputDirectory"
    }

    throw "固定发布输入目录下发现多个 air.cloud.airtunnel.exe，请清理重复发布目录: $InputDirectory"
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $Destination $_.Name) -Recurse -Force
    }
}

function Get-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Agent export package is missing required file: $RelativePath"
    }

    return $path
}

function Get-AgentSettings {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Configuration
    )

    $reverseTunnelProperty = $Configuration.PSObject.Properties["ReverseTunnelSettings"]
    $settings = $null
    if ($null -ne $reverseTunnelProperty) {
        $settings = $reverseTunnelProperty.Value
    }
    if ($null -eq $settings) {
        $settings = $Configuration
    }

    if ($null -eq $settings.AgentCertificate) {
        throw "Agent export configuration does not contain ReverseTunnelSettings.AgentCertificate."
    }

    return $settings
}

function Set-WindowsAgentCertificatePaths {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Configuration
    )

    $settings = Get-AgentSettings $Configuration
    $settings.AgentCertificate.CertificatePath = "data/certs/agent.crt"
    $settings.AgentCertificate.PrivateKeyPath = "data/certs/agent.key"
    $settings.AgentCertificate.TrustedIssuerPath = "data/certs/ca.crt"
    return $settings
}

function ConvertTo-AgentRuntimeConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Configuration
    )

    # The Server export endpoint serializes ReverseTunnelOptions itself, while
    # the standalone host loads that object from the ReverseTunnelSettings
    # configuration section. Wrap a flat export before writing the Windows
    # runtime package so the executable receives the intended Agent settings.
    if ($null -ne $Configuration.PSObject.Properties["ReverseTunnelSettings"]) {
        return $Configuration
    }

    return [pscustomobject]@{
        ReverseTunnelSettings = $Configuration
    }
}

function Get-SafeFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $invalidCharacters = [System.IO.Path]::GetInvalidFileNameChars()
    $builder = [System.Text.StringBuilder]::new()
    foreach ($character in $Value.Trim()) {
        if ($invalidCharacters -contains $character) {
            [void]$builder.Append("_")
        }
        else {
            [void]$builder.Append($character)
        }
    }

    if ($builder.Length -eq 0) {
        return "agent"
    }

    return $builder.ToString()
}

function Select-AgentExportPackage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CertificatesDirectory
    )

    $certificatePackages = @(Get-ChildItem -LiteralPath $CertificatesDirectory -File -Filter "*.zip" |
        Sort-Object LastWriteTime -Descending)
    if ($certificatePackages.Count -eq 0) {
        throw "No certificate export packages were found in: $CertificatesDirectory"
    }

    Write-Host "Certificate export packages (newest first):"
    for ($index = 0; $index -lt $certificatePackages.Count; $index++) {
        $package = $certificatePackages[$index]
        Write-Host (" [{0}] {1}  ({2:yyyy-MM-dd HH:mm:ss})" -f ($index + 1), $package.Name, $package.LastWriteTime)
    }

    if ([Console]::IsInputRedirected) {
        throw "Certificate selection requires an interactive PowerShell console. Pass -AgentExportPackage for non-interactive use."
    }

    while ($true) {
        $choice = Read-Host "Select a certificate package by number"
        $selectedIndex = 0
        if ([int]::TryParse($choice, [ref]$selectedIndex) -and
            $selectedIndex -ge 1 -and $selectedIndex -le $certificatePackages.Count) {
            return $certificatePackages[$selectedIndex - 1].FullName
        }

        Write-Warning "Please enter a number from 1 to $($certificatePackages.Count)."
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$certificatesDirectory = Join-Path $scriptRoot "certificates"
$publishInputDirectory = Join-Path $scriptRoot "publish-input\win-x64"
$publishsDirectory = Join-Path $scriptRoot "publishs"
New-Item -ItemType Directory -Force -Path $certificatesDirectory, $publishsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $publishInputDirectory | Out-Null

if ([string]::IsNullOrWhiteSpace($UserName)) {
    $UserName = Read-Host "请输入使用人名称"
}
if ([string]::IsNullOrWhiteSpace($UserName)) {
    throw "使用人名称不能为空。"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Read-Host "请输入版本号"
}
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "版本号不能为空。"
}

if ([string]::IsNullOrWhiteSpace($AgentExportPackage)) {
    $AgentExportPackage = Select-AgentExportPackage -CertificatesDirectory $certificatesDirectory
}

$agentExportPath = Resolve-FullPath $AgentExportPackage
if (-not [string]::IsNullOrWhiteSpace($PublishInput)) {
    $requestedPublishInputPath = [System.IO.Path]::GetFullPath($PublishInput)
    $expectedPublishInputPath = [System.IO.Path]::GetFullPath($publishInputDirectory)
    if (-not $requestedPublishInputPath.Equals($expectedPublishInputPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "PublishInput is fixed to the script directory's publish-input\win-x64: $expectedPublishInputPath"
    }
}
if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $requestedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
    $expectedOutputDirectory = [System.IO.Path]::GetFullPath($publishsDirectory)
    if (-not $requestedOutputDirectory.Equals($expectedOutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "OutputDirectory is fixed to the script directory's publishs: $expectedOutputDirectory"
    }
}

$publishInputPath = Resolve-FullPath $publishInputDirectory
$nssmInputPath = $null
if (-not [string]::IsNullOrWhiteSpace($NssmPath)) {
    $nssmInputPath = Resolve-FullPath $NssmPath
    if (-not (Test-Path -LiteralPath $nssmInputPath -PathType Leaf)) {
        throw "NssmPath does not point to a file: $nssmInputPath"
    }
}

if (-not [System.IO.Path]::GetExtension($agentExportPath).Equals(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "AgentExportPackage must be a ZIP file: $agentExportPath"
}

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("airtunnel-windows-agent-" + [Guid]::NewGuid().ToString("N"))
$publishInputRoot = $publishInputPath
$publishArchiveRoot = $null
if ([System.IO.Path]::GetExtension($publishInputPath).Equals(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
    $publishArchiveRoot = Join-Path $temporaryRoot "publish"
    Expand-SafeZip -ZipPath $publishInputPath -Destination $publishArchiveRoot
    $publishInputRoot = $publishArchiveRoot
}

$publishRoot = Get-PublishRoot $publishInputRoot
$publishExecutable = Join-Path $publishRoot "air.cloud.airtunnel.exe"
$publishRuntimeConfig = Join-Path $publishRoot "air.cloud.airtunnel.runtimeconfig.json"
if (-not (Test-Path -LiteralPath $publishRuntimeConfig -PathType Leaf)) {
    throw "PublishDirectory is missing air.cloud.airtunnel.runtimeconfig.json: $publishRoot"
}

if ($null -eq $nssmInputPath) {
    $defaultNssmCandidates = @(
        (Join-Path $scriptRoot "nssm.exe"),
        (Join-Path $publishInputDirectory "nssm.exe"),
        (Join-Path $publishRoot "nssm.exe")
    )
    $nssmInputPath = $defaultNssmCandidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
    if ($null -ne $nssmInputPath) {
        $nssmInputPath = Resolve-FullPath $nssmInputPath
    }
}

$exportRoot = Join-Path $temporaryRoot "export"
$outputPath = $null

try {
    Expand-SafeZip -ZipPath $agentExportPath -Destination $exportRoot

    $exportConfigurationPath = Get-RequiredFile -Root $exportRoot -RelativePath "config/00-airtunnel-agent.json"
    $exportCaPath = Get-RequiredFile -Root $exportRoot -RelativePath "data/certs/ca.crt"

    $privateMaterial = @(Get-ChildItem -LiteralPath $exportRoot -Recurse -File |
        Where-Object {
            $_.Name -match '(?i)(^|[._-])(agent\.key|agent\.crt|ca\.key|ca\.pfx|private)'
        })
    if ($privateMaterial.Count -gt 0) {
        $names = $privateMaterial | ForEach-Object { $_.FullName }
        throw "Agent export package contains private certificate material: $($names -join ', ')"
    }

    $configuration = Get-Content -LiteralPath $exportConfigurationPath -Raw | ConvertFrom-Json
    $configuration = ConvertTo-AgentRuntimeConfiguration -Configuration $configuration
    $agentSettings = Set-WindowsAgentCertificatePaths -Configuration $configuration
    $agentId = [string]$agentSettings.AgentId
    if ([string]::IsNullOrWhiteSpace($agentId)) {
        throw "Agent export configuration has an empty ReverseTunnelSettings.AgentId."
    }

    $safeAgentId = Get-SafeFileName $agentId
    $safeUserName = Get-SafeFileName $UserName
    $safeVersion = Get-SafeFileName $Version
    $publishStamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
    $packageName = "airtunnel-agent-windows-{0}-{1}-{2}-{3}" -f $safeUserName, $safeVersion, $safeAgentId, $publishStamp
    $outputPath = Join-Path $publishsDirectory $packageName
    if (Test-Path -LiteralPath $outputPath) {
        $packageName = "$packageName-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
        $outputPath = Join-Path $publishsDirectory $packageName
    }

    $outputPath = [System.IO.Path]::GetFullPath($outputPath)
    if (Test-Path -LiteralPath $outputPath) {
        if (-not $Force) {
            throw "OutputDirectory already exists. Use -Force to replace it: $outputPath"
        }

        Remove-Item -LiteralPath $outputPath -Recurse -Force
    }

    $appDirectory = Join-Path $outputPath "app"
    New-Item -ItemType Directory -Force -Path $appDirectory | Out-Null
    Copy-DirectoryContents -Source $publishRoot -Destination $appDirectory

    $appConfigDirectory = Join-Path $appDirectory "config"
    $appDataDirectory = Join-Path $appDirectory "data"
    $appCertificateDirectory = Join-Path $appDataDirectory "certs"
    $appLogsDirectory = Join-Path $appDirectory "logs"
    New-Item -ItemType Directory -Force -Path $appConfigDirectory, $appCertificateDirectory, $appLogsDirectory | Out-Null

    Copy-Item -LiteralPath $exportCaPath -Destination (Join-Path $appCertificateDirectory "ca.crt") -Force
    $configuration | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $appConfigDirectory "00-airtunnel-agent.json") -Encoding UTF8

    if ($null -ne $nssmInputPath) {
        Copy-Item -LiteralPath $nssmInputPath -Destination (Join-Path $outputPath "nssm.exe") -Force

        $serviceName = "AirTunnel-Agent-" + ($agentId -replace "[^A-Za-z0-9_.-]", "-")
        $displayName = "AirTunnel Agent ($agentId)"
        $installServiceScript = @'
param(
    [switch]$DoNotStart
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($currentIdentity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Current PowerShell is not elevated. Please run as Administrator."
    }
}

function Invoke-Nssm {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $script:nssmPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "NSSM command failed with exit code ${LASTEXITCODE}: nssm $($Arguments -join ' ')"
    }
}

Assert-Administrator

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:nssmPath = Join-Path $root "nssm.exe"
$appDirectory = Join-Path $root "app"
$executablePath = Join-Path $appDirectory "air.cloud.airtunnel.exe"
$logDirectory = Join-Path $appDirectory "logs"
$serviceName = "__SERVICE_NAME__"
$displayName = "__DISPLAY_NAME__"

foreach ($path in @($script:nssmPath, $executablePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file was not found: $path"
    }
}

New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
if ($null -ne (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    throw "The Windows service '$serviceName' already exists. Run uninstall-agent-service.ps1 first."
}

$created = $false
try {
    Invoke-Nssm -Arguments @("install", $serviceName, $executablePath)
    $created = $true

    $arguments = '--contentRoot "' + $appDirectory + '" --environment Production'
    Invoke-Nssm -Arguments @("set", $serviceName, "AppDirectory", $appDirectory)
    Invoke-Nssm -Arguments @("set", $serviceName, "AppParameters", $arguments)
    Invoke-Nssm -Arguments @("set", $serviceName, "DisplayName", $displayName)
    Invoke-Nssm -Arguments @("set", $serviceName, "Description", "AirTunnel Agent")
    Invoke-Nssm -Arguments @("set", $serviceName, "Start", "SERVICE_AUTO_START")
    Invoke-Nssm -Arguments @("set", $serviceName, "AppExit", "Default", "Restart")
    Invoke-Nssm -Arguments @("set", $serviceName, "AppRestartDelay", "5000")
    Invoke-Nssm -Arguments @("set", $serviceName, "AppStdout", (Join-Path $logDirectory "service.stdout.log"))
    Invoke-Nssm -Arguments @("set", $serviceName, "AppStderr", (Join-Path $logDirectory "service.stderr.log"))

    if (-not $DoNotStart) {
        Start-Service -Name $serviceName
    }

    Write-Host "Installed Windows service: $serviceName"
}
catch {
    if ($created) {
        & $script:nssmPath remove $serviceName confirm | Out-Null
    }

    throw
}
'@
        $installServiceScript = $installServiceScript.Replace("__SERVICE_NAME__", $serviceName).Replace("__DISPLAY_NAME__", $displayName)
        Set-Content -LiteralPath (Join-Path $outputPath "install-agent-service.ps1") -Value $installServiceScript -Encoding UTF8

        $uninstallServiceScript = @'
$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($currentIdentity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Current PowerShell is not elevated. Please run as Administrator."
    }
}

function Invoke-Nssm {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $script:nssmPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "NSSM command failed with exit code ${LASTEXITCODE}: nssm $($Arguments -join ' ')"
    }
}

Assert-Administrator

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:nssmPath = Join-Path $root "nssm.exe"
$serviceName = "__SERVICE_NAME__"
if (-not (Test-Path -LiteralPath $script:nssmPath -PathType Leaf)) {
    throw "NSSM was not found: $script:nssmPath"
}

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -eq $existingService) {
    Write-Host "Windows service '$serviceName' is not installed."
    exit 0
}

if ($existingService.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    $existingService.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Stopped, [TimeSpan]::FromSeconds(15))
}

Invoke-Nssm -Arguments @("remove", $serviceName, "confirm")
Write-Host "Removed Windows service: $serviceName"
'@
        $uninstallServiceScript = $uninstallServiceScript.Replace("__SERVICE_NAME__", $serviceName)
        Set-Content -LiteralPath (Join-Path $outputPath "uninstall-agent-service.ps1") -Value $uninstallServiceScript -Encoding UTF8
    }

    $readme = @"
# AirTunnel Agent Windows Runtime

User: $UserName
AgentId: $agentId
Version: $Version
Runtime: win-x64 self-contained

## Start

Run .\start-agent.ps1 from this directory. The script starts app\air.cloud.airtunnel.exe
with app as the working directory so the merged Agent configuration under
app\config and certificate files under app\data\certs are loaded.

When nssm.exe is included, run install-agent-service.ps1 from an elevated
PowerShell window to install the Agent as a Windows background service. Run
uninstall-agent-service.ps1 to remove that service.

The package contains the Server CA public certificate at app\data\certs\ca.crt.
The Agent creates agent.crt and agent.key there after successful enrollment.
The package does not contain CA private keys or Agent private keys.

## Before first start

1. Confirm the Agent can reach the configured ServerAddress.
2. Confirm the Server has this AgentId authorized.
3. Review app\config\00-airtunnel-agent.json and the LocalServices entries.

The original Server export package is recorded by SHA-256 in
airtunnel-agent.windows.manifest.json.
"@
    Set-Content -LiteralPath (Join-Path $outputPath "README-Windows.md") -Value $readme -Encoding UTF8

    $launcher = @'
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$app = Join-Path $root "app"
Set-Location -LiteralPath $app
& (Join-Path $app "air.cloud.airtunnel.exe") --contentRoot $app --environment Production
exit $LASTEXITCODE
'@
    Set-Content -LiteralPath (Join-Path $outputPath "start-agent.ps1") -Value $launcher -Encoding UTF8

    $publishHash = (Get-FileHash -LiteralPath $publishExecutable -Algorithm SHA256).Hash
    $exportHash = (Get-FileHash -LiteralPath $agentExportPath -Algorithm SHA256).Hash
    $manifest = [ordered]@{
        name = "airtunnel-agent-windows"
        packageName = $packageName
        userName = $UserName
        agentId = $agentId
        version = $Version
        runtime = "win-x64"
        selfContained = $true
        createdAtUtc = (Get-Date).ToUniversalTime().ToString("O")
        entrypoint = "start-agent.ps1"
        executable = "app/air.cloud.airtunnel.exe"
        configuration = "app/config/00-airtunnel-agent.json"
        certificateFiles = @(
            "app/data/certs/ca.crt",
            "app/data/certs/agent.crt",
            "app/data/certs/agent.key"
        )
        sourceAgentExport = [ordered]@{
            fileName = [System.IO.Path]::GetFileName($agentExportPath)
            sha256 = $exportHash
        }
        sourcePublish = [ordered]@{
            input = $publishInputPath
            executableSha256 = $publishHash
        }
        windowsService = if ($null -ne $nssmInputPath) {
            [ordered]@{
                name = $serviceName
                installScript = "install-agent-service.ps1"
                uninstallScript = "uninstall-agent-service.ps1"
                nssm = "nssm.exe"
            }
        }
        else {
            $null
        }
        requiredFiles = @(
            "app/air.cloud.airtunnel.exe",
            "app/config/00-airtunnel-agent.json",
            "app/data/certs/ca.crt"
        )
    }
    $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputPath "airtunnel-agent.windows.manifest.json") -Encoding UTF8

    $zipPath = "$outputPath.zip"
    if (Test-Path -LiteralPath $zipPath) {
        if (-not $Force) {
            throw "Output ZIP already exists. Use -Force to replace it: $zipPath"
        }

        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $zipPath -Force

    Write-Host "AirTunnel Windows Agent runtime package:"
    Write-Host " - Directory: $outputPath"
    Write-Host " - Zip: $zipPath"
    Write-Host " - User: $UserName"
    Write-Host " - AgentId: $agentId"
    Write-Host " - Version: $Version"
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
