param
(
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$DestinationRoot
)

# Normalize paths
$SourceRoot = (Resolve-Path $SourceRoot).Path
$DestinationRoot = (Resolve-Path -Path $DestinationRoot -ErrorAction SilentlyContinue)?.Path `
    ?? (New-Item -ItemType Directory -Path $DestinationRoot).FullName

# File patterns to include
$includeExtensions = @(
    ".cs",
    ".csproj",
    ".slnx"
)

Get-ChildItem -Path $SourceRoot -Recurse -File | ForEach-Object {
    if ($includeExtensions -contains $_.Extension.ToLowerInvariant()) {

        # Compute relative path
        $relativePath = $_.FullName.Substring($SourceRoot.Length).TrimStart('\')

        # Compute destination file path
        $destinationPath = Join-Path $DestinationRoot $relativePath

        # Ensure destination directory exists
        $destinationDir = Split-Path $destinationPath -Parent
        if (-not (Test-Path $destinationDir)) {
            New-Item -ItemType Directory -Path $destinationDir | Out-Null
        }

        # Copy file
        Copy-Item -Path $_.FullName -Destination $destinationPath -Force
    }
}

Write-Host "Extraction complete."
Write-Host "Source:      $SourceRoot"
Write-Host "Destination: $DestinationRoot"
