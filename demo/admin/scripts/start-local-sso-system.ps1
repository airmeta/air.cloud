param(
    [string]$NodeVersion = '22.22.0'
)

$ErrorActionPreference = 'Stop'
$adminRoot = Split-Path -Parent $PSScriptRoot

function Test-ListeningPort {
    param([int]$Port)

    return $null -ne (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue)
}

function Start-AdminApp {
    param(
        [string]$Name,
        [int]$Port,
        [string]$Workspace
    )

    if (Test-ListeningPort -Port $Port) {
        Write-Warning "$Name is already listening on http://localhost:$Port"
        return
    }

    $escapedRoot = $adminRoot.Replace("'", "''")
    $command = "nvm use $NodeVersion; Set-Location '$escapedRoot'; Write-Host 'Starting $Name on http://localhost:$Port'; pnpm -F '$Workspace' run dev"
    $process = Start-Process powershell.exe -WorkingDirectory $adminRoot -ArgumentList @(
        '-NoExit',
        '-Command',
        $command
    ) -PassThru

    Write-Host "$Name PowerShell started with PID $($process.Id)"
}

& nvm use $NodeVersion
Start-AdminApp -Name 'Air.Cloud SSO' -Port 6103 -Workspace '@air-cloud/sso'
Start-AdminApp -Name 'Air.Cloud System' -Port 6101 -Workspace '@air-cloud/system'
