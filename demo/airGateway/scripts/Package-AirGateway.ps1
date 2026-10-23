param(
    [string]$Configuration = "Release",
    [string]$Version = "latest",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$gatewayRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$repoRoot = (Resolve-Path (Join-Path $gatewayRoot "..\..")).Path
$projectPath = Join-Path $gatewayRoot "air.gateway\air.gateway.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "builds\air-cloud-gateway-$Version"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoFullPath = [System.IO.Path]::GetFullPath($repoRoot)
if (-not $resolvedOutput.StartsWith($repoFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay inside the repository: $resolvedOutput"
}

if (Test-Path -LiteralPath $resolvedOutput) {
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null
$publishDirectory = Join-Path $resolvedOutput "publish"
dotnet publish $projectPath -c $Configuration -o $publishDirectory --no-restore -v:minimal /p:UseAppHost=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for $projectPath"
}

Copy-Item -LiteralPath (Join-Path $gatewayRoot "Dockerfile.runtime") -Destination (Join-Path $resolvedOutput "Dockerfile") -Force
Copy-Item -LiteralPath (Join-Path $gatewayRoot "docker-compose.yml") -Destination (Join-Path $resolvedOutput "docker-compose.yml") -Force
Copy-Item -LiteralPath (Join-Path $gatewayRoot "README.md") -Destination (Join-Path $resolvedOutput "README.md") -Force
Copy-Item -LiteralPath (Join-Path $gatewayRoot "config") -Destination (Join-Path $resolvedOutput "config") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $gatewayRoot "plugins") -Destination (Join-Path $resolvedOutput "plugins") -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "runtime") | Out-Null
Copy-Item -LiteralPath (Join-Path $gatewayRoot "air.gateway\appsettings.json") -Destination (Join-Path $resolvedOutput "runtime\appsettings.json") -Force
Copy-Item -LiteralPath (Join-Path $gatewayRoot "air.gateway\appsettings.json") -Destination (Join-Path $resolvedOutput "runtime\appsettings.json.example") -Force
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "data\control-plane-auth") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "logs") | Out-Null

$manifest = [ordered]@{
    name = "air-cloud-gateway"
    version = $Version
    createdAt = (Get-Date).ToString("O")
    image = "air-cloud-gateway:$Version"
    container = "air-cloud-gateway"
    composeFile = "docker-compose.yml"
    ports = @(
        "18081:8080/tcp",
        "18082:8081/tcp"
    )
    configFile = "config/00-airgateway.json"
    controlPlaneConfigFile = "config/control-plane-auth.json"
    requiredMountedFiles = @()
    requiredFiles = @()
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $resolvedOutput "air-cloud-gateway.manifest.json") -Encoding UTF8

$zipPath = "$resolvedOutput.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
$packageItems = Get-ChildItem -LiteralPath $resolvedOutput -Force
Compress-Archive -LiteralPath $packageItems.FullName -DestinationPath $zipPath -Force

# Compress-Archive stores non-empty directories through their child paths only.
# Add an explicit runtime directory entry so archive viewers show the required mount directory.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipArchive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    if (-not ($zipArchive.Entries.FullName -contains "runtime/")) {
        [void]$zipArchive.CreateEntry("runtime/")
    }
}
finally {
    $zipArchive.Dispose()
}

Write-Host "Air Cloud Gateway package:"
Write-Host " - Directory: $resolvedOutput"
Write-Host " - Zip: $zipPath"
Write-Host " - docker build -t air-cloud-gateway:$Version ."
