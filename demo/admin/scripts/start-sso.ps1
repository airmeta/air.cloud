param(
    [string]$NodeVersion = '22.22.0'
)

$ErrorActionPreference = 'Stop'
$adminRoot = Split-Path -Parent $PSScriptRoot

if (Get-NetTCPConnection -State Listen -LocalPort 6103 -ErrorAction SilentlyContinue) {
    Write-Warning 'SSO is already listening on http://localhost:6103'
    return
}

& nvm use $NodeVersion
Set-Location $adminRoot
pnpm -F '@air-cloud/sso' run dev
