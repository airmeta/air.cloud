param(
    [string]$Configuration = "Release",
    [string]$Version = "10.0.0",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$airTunnelRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$repoRoot = (Resolve-Path (Join-Path $airTunnelRoot "..\..")).Path
$projectPath = Join-Path $airTunnelRoot "air.cloud.airtunnel\air.cloud.airtunnel.csproj"
$templateRoot = Join-Path $airTunnelRoot "deploy\client"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "builds\airtunnel-agent-$Version"
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
$composePath = Join-Path $resolvedOutput "docker-compose.yml"
Copy-Item -LiteralPath (Join-Path $templateRoot "docker-compose.yml") -Destination $composePath -Force
$compose = (Get-Content -LiteralPath $composePath -Raw).Replace("airtunnel-agent:1.0.0.0", "airtunnel-agent:$Version")
Set-Content -LiteralPath $composePath -Value $compose -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $templateRoot "README.md") -Destination (Join-Path $resolvedOutput "README.md") -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "config") -Destination (Join-Path $resolvedOutput "config") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $templateRoot "configs") -Destination (Join-Path $resolvedOutput "configs") -Recurse -Force

New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "data\certs") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $resolvedOutput "logs") | Out-Null

$manifest = [ordered]@{
    name = "airtunnel-agent"
    version = $Version
    createdAt = (Get-Date).ToString("O")
    image = "airtunnel-agent:$Version"
    container = "airtunnel-agent"
    composeFile = "docker-compose.yml"
    configFile = "config/00-airtunnel-agent.json"
    requiredMountedFiles = @(
        "data/certs/ca.crt"
    )
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $resolvedOutput "airtunnel-agent.manifest.json") -Encoding UTF8

$zipPath = "$resolvedOutput.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$packageItems = Get-ChildItem -LiteralPath $resolvedOutput -Force
Compress-Archive -LiteralPath $packageItems.FullName -DestinationPath $zipPath -Force

Write-Host "AirTunnel client package:"
Write-Host " - Directory: $resolvedOutput"
Write-Host " - Zip: $zipPath"
Write-Host "Build image after unzip:"
Write-Host " - docker build -t airtunnel-agent:$Version ."
