param (
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

# Normalize paths
$SourceRoot = (Resolve-Path $SourceRoot).Path
$OutputFile = Resolve-Path -Path $OutputFile

# Extensions to include
$includeExtensions = @(
    ".cs",
    ".csproj",
    ".slnx"
)

# Collect and sort files by relative path
$files = Get-ChildItem -Path $SourceRoot -Recurse -File |
    Where-Object { $includeExtensions -contains $_.Extension.ToLowerInvariant() } |
    Sort-Object {
        $_.FullName.Substring($SourceRoot.Length)
    }

# Clear output file
Set-Content -Path $OutputFile -Value "" -Encoding UTF8

foreach ($file in $files) {

    $relativePath = $file.FullName.Substring($SourceRoot.Length).TrimStart('\')

    Add-Content -Path $OutputFile -Encoding UTF8 -Value @"
================================================================================
FILE: $relativePath
================================================================================

"@

    Get-Content -Path $file.FullName -Encoding UTF8 |
        Add-Content -Path $OutputFile -Encoding UTF8

    Add-Content -Path $OutputFile -Encoding UTF8 -Value "`r`n`r`n"
}

Write-Host "Concatenation complete."
Write-Host "Source: $SourceRoot"
Write-Host "Output: $OutputFile"