param(
    [string]$NodeVersion = '22.22.0'
)

$ErrorActionPreference = 'Stop'
$adminRoot = Split-Path -Parent $PSScriptRoot

if (Get-NetTCPConnection -State Listen -LocalPort 6101 -ErrorAction SilentlyContinue) {
    Write-Warning 'System is already listening on http://localhost:6101'
    return
}

& nvm use $NodeVersion
Set-Location $adminRoot
pnpm -F '@air-cloud/system' run dev
