param(
    [Parameter(Mandatory = $true)]
    [string]$ServerConfig,

    [Parameter(Mandatory = $true)]
    [string]$CertificateDirectory,

    [string]$CaCertificatePath = "",

    [string]$CaPrivateKeyPath = "",

    [string]$PfxPasswordEnvironmentVariable = "ASPNETCORE_Kestrel__Certificates__Default__Password",

    [string]$OpenSslPath = "openssl",

    [ValidateRange(1, 3650)]
    [int]$ValidityDays = 825,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-ExistingPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not found: $Path"
    }

    return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
}

function Get-ServerCertificateConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Configuration
    )

    $settings = $Configuration.PSObject.Properties["ReverseTunnelSettings"]?.Value
    if ($null -eq $settings) {
        $settings = $Configuration
    }

    $certificate = $settings.PSObject.Properties["ServerCertificate"]?.Value
    if ($null -eq $certificate) {
        throw "Server configuration does not contain ReverseTunnelSettings.ServerCertificate."
    }

    $commonName = [string]$certificate.CommonName
    $names = @($certificate.SubjectAlternativeNames | ForEach-Object { [string]$_ })
    $commonName = $commonName.Trim()
    $names = @($names |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique)

    if ([string]::IsNullOrWhiteSpace($commonName) -and $names.Count -gt 0) {
        $commonName = $names[0]
    }

    if ([string]::IsNullOrWhiteSpace($commonName)) {
        throw "ReverseTunnelSettings.ServerCertificate.CommonName is required."
    }

    if ($names.Count -eq 0) {
        $names = @($commonName)
    }

    return [pscustomobject]@{
        CommonName = $commonName
        SubjectAlternativeNames = $names
    }
}

function Test-CertificateHostName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $ipAddress = $null
    return [System.Net.IPAddress]::TryParse($Value, [ref]$ipAddress) -or
        [Uri]::CheckHostName($Value) -in @([UriHostNameType]::Dns, [UriHostNameType]::IPv4, [UriHostNameType]::IPv6)
}

function Get-OpenSslSanLine {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Names
    )

    $items = foreach ($name in $Names) {
        if (-not (Test-CertificateHostName $name)) {
            throw "ServerCertificate SAN value is not an IP address or DNS name: $name"
        }

        $ipAddress = $null
        if ([System.Net.IPAddress]::TryParse($name, [ref]$ipAddress)) {
            "IP:$name"
        }
        else {
            "DNS:$name"
        }
    }

    return "subjectAltName=" + ($items -join ",")
}

function Invoke-OpenSsl {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $OpenSslPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "OpenSSL command failed with exit code ${LASTEXITCODE}: openssl $($Arguments -join ' ')"
    }
}

$configPath = Resolve-ExistingPath -Path $ServerConfig -Description "Server configuration"
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$certificate = Get-ServerCertificateConfiguration -Configuration $config

$outputDirectory = [System.IO.Path]::GetFullPath($CertificateDirectory)
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

if ([string]::IsNullOrWhiteSpace($CaCertificatePath)) {
    $CaCertificatePath = Join-Path $outputDirectory "ca.crt"
}
if ([string]::IsNullOrWhiteSpace($CaPrivateKeyPath)) {
    $CaPrivateKeyPath = Join-Path $outputDirectory "ca.key"
}
$caCertificatePath = Resolve-ExistingPath -Path $CaCertificatePath -Description "CA certificate"
$caPrivateKeyPath = Resolve-ExistingPath -Path $CaPrivateKeyPath -Description "CA private key"

$serverCertificatePath = Join-Path $outputDirectory "server.crt"
$serverPrivateKeyPath = Join-Path $outputDirectory "server.key"
$serverCsrPath = Join-Path $outputDirectory "server.csr"
$serverExtensionPath = Join-Path $outputDirectory "server.ext"
$serverPfxPath = Join-Path $outputDirectory "server.pfx"
$serialPath = Join-Path $outputDirectory ".server-ca.srl"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDirectory = Join-Path $outputDirectory "server-certificate-backup-$timestamp"
$existingOutputs = @(
    $serverCertificatePath,
    $serverPrivateKeyPath,
    $serverCsrPath,
    $serverExtensionPath,
    $serverPfxPath
) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }

if ($existingOutputs.Count -gt 0 -and -not $Force) {
    throw "Server certificate files already exist. Use -Force to back them up and replace them: $($existingOutputs -join ', ')"
}

if ($existingOutputs.Count -gt 0) {
    New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null
    foreach ($file in $existingOutputs) {
        Copy-Item -LiteralPath $file -Destination (Join-Path $backupDirectory (Split-Path -Leaf $file)) -Force
    }
}

$escapedCommonName = $certificate.CommonName.Replace("/", "\/", [StringComparison]::Ordinal)
$extensionContent = @(
    (Get-OpenSslSanLine -Names $certificate.SubjectAlternativeNames)
    "extendedKeyUsage=serverAuth"
    "keyUsage=digitalSignature,keyEncipherment"
    "basicConstraints=CA:FALSE"
)
$extensionContent | Set-Content -LiteralPath $serverExtensionPath -Encoding ASCII

Invoke-OpenSsl -Arguments @(
    "genrsa",
    "-out", $serverPrivateKeyPath,
    "2048"
)
Invoke-OpenSsl -Arguments @(
    "req",
    "-new",
    "-key", $serverPrivateKeyPath,
    "-out", $serverCsrPath,
    "-subj", "/CN=$escapedCommonName"
)
Invoke-OpenSsl -Arguments @(
    "x509",
    "-req",
    "-in", $serverCsrPath,
    "-CA", $caCertificatePath,
    "-CAkey", $caPrivateKeyPath,
    "-CAcreateserial",
    "-CAserial", $serialPath,
    "-out", $serverCertificatePath,
    "-days", $ValidityDays.ToString(),
    "-sha256",
    "-extfile", $serverExtensionPath
)

if (-not [string]::IsNullOrWhiteSpace($PfxPasswordEnvironmentVariable)) {
    $pfxPassword = [Environment]::GetEnvironmentVariable($PfxPasswordEnvironmentVariable)
    if (-not [string]::IsNullOrEmpty($pfxPassword)) {
        Invoke-OpenSsl -Arguments @(
            "pkcs12",
            "-export",
            "-out", $serverPfxPath,
            "-inkey", $serverPrivateKeyPath,
            "-in", $serverCertificatePath,
            "-certfile", $caCertificatePath,
            "-passout", "env:$PfxPasswordEnvironmentVariable"
        )
    }
    else {
        Write-Warning "PFX was not generated because environment variable '$PfxPasswordEnvironmentVariable' is empty."
    }
}

if (Test-Path -LiteralPath $serialPath -PathType Leaf) {
    Remove-Item -LiteralPath $serialPath -Force
}

Write-Host "AirTunnel Server certificate generated:"
Write-Host " - CommonName: $($certificate.CommonName)"
Write-Host " - SAN: $($certificate.SubjectAlternativeNames -join ', ')"
Write-Host " - Certificate: $serverCertificatePath"
Write-Host " - Private key: $serverPrivateKeyPath"
if (Test-Path -LiteralPath $serverPfxPath -PathType Leaf) {
    Write-Host " - PFX: $serverPfxPath"
}
if ($existingOutputs.Count -gt 0) {
    Write-Host " - Backup: $backupDirectory"
}
