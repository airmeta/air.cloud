<#
.SYNOPSIS
    Finds C# files without the required copyright header.

.DESCRIPTION
    Checks the beginning of every .cs or .csx file in the repository, including
    untracked files. A file is compliant
    when its opening comment block contains "Copyright (c) 2024-2030".
    Missing file paths are written to a UTF-8 text file relative to the repository root.
#>
[CmdletBinding()]
param(
    [string]$OutputFile = "missing-copyright-headers.txt"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) {
    throw "Run this script from inside a Git repository."
}

function Get-SourceFiles {
    param(
        [Parameter(Mandatory)]
        [string]$RootPath
    )

    $excludedDirectoryNames = @(".git", ".claude", ".codex", ".vs", "bin", "obj", "node_modules")
    $directories = [System.Collections.Generic.Stack[System.IO.DirectoryInfo]]::new()
    $directories.Push([System.IO.DirectoryInfo]::new($RootPath))

    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        if ($directory.Name -in $excludedDirectoryNames) {
            continue
        }

        foreach ($file in $directory.GetFiles()) {
            if ($file.Extension -in @(".cs", ".csx")) {
                $file
            }
        }

        foreach ($subdirectory in $directory.GetDirectories()) {
            $directories.Push($subdirectory)
        }
    }
}

Push-Location $repositoryRoot
try {
    $outputPath = Join-Path $repositoryRoot $OutputFile
    $sourceFiles = Get-SourceFiles -RootPath $repositoryRoot

    $missingHeaders = foreach ($sourceFile in $sourceFiles) {
        $header = (Get-Content -LiteralPath $sourceFile.FullName -Encoding UTF8 -TotalCount 12) -join "`n"

        # The header must start the file, allowing only a UTF-8 BOM before "/*".
        if ($header -notmatch '^(?:\uFEFF)?/\*\s*\r?\n\s*\*\s*Copyright \(c\) 2024-2030\b') {
            $sourceFile.FullName.Substring($repositoryRoot.Length).TrimStart('\', '/')
        }
    }

    $missingHeaders | Set-Content -LiteralPath $outputPath -Encoding utf8
    Write-Host "Found $(@($missingHeaders).Count) file(s) without the copyright header."
    Write-Host "Report: $outputPath"
}
finally {
    Pop-Location
}
