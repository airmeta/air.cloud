param(
    [string]$Configuration = "Release",
    [string]$Version = "latest",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$airTunnelRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$repoRoot = (Resolve-Path (Join-Path $airTunnelRoot "..\..")).Path
$projectPath = Join-Path $airTunnelRoot "air.cloud.airtunnel\air.cloud.airtunnel.csproj"
$templateRoot = Join-Path $airTunnelRoot "deploy\server"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "builds\air-cloud-airtunnel-server-$Version"
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

Copy-Item -LiteralPath (Join-Path $templateRoot "Dockerfile") -Destination (Join-Path $resolvedOutput "Dockerfile") -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "docker-compose.server.yml") -Destination (Join-Path $resolvedOutput "docker-compose.server.yml") -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "README.md") -Destination (Join-Path $resolvedOutput "README.md") -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "config") -Destination (Join-Path $resolvedOutput "config") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "runtime") -Destination (Join-Path $resolvedOutput "runtime") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "secrets") -Destination (Join-Path $resolvedOutput "secrets") -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "data\airtunnel"), (Join-Path $resolvedOutput "data\airtunnel-config"), (Join-Path $resolvedOutput "data\airtunnel-config-backups"), (Join-Path $resolvedOutput "logs") | Out-Null

$manifest = [ordered]@{
    name = "air-cloud-airtunnel-server"
    host = "air.cloud.airtunnel"
    version = $Version
    createdAt = (Get-Date).ToString("O")
    image = "air-cloud-airtunnel-server:$Version"
    container = "air-cloud-airtunnel-server"
    composeFile = "docker-compose.server.yml"
    ports = @(
        "15671/tcp",
        "15671/udp",
        "18083:8080/tcp",
        "18084:8081/tcp",
        "18443:18443/tcp"
    )
    configFile = "config/20-airtunnel-server.json"
    requiredMountedFiles = @(
        "data/airtunnel/server.crt",
        "data/airtunnel/server.key",
        "data/airtunnel/ca.pfx",
        "data/airtunnel/ca.crt",
        "runtime/appsettings.json"
    )
    requiredFiles = @(
        "secrets/server.env"
    )
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $resolvedOutput "air-cloud-airtunnel-server.manifest.json") -Encoding UTF8

$zipPath = "$resolvedOutput.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
$packageItems = Get-ChildItem -LiteralPath $resolvedOutput -Force
Compress-Archive -LiteralPath $packageItems.FullName -DestinationPath $zipPath -Force

Write-Host "Standalone AirTunnel Server package:"
Write-Host " - Directory: $resolvedOutput"
Write-Host " - Zip: $zipPath"
Write-Host " - docker build -t air-cloud-airtunnel-server:$Version ."
