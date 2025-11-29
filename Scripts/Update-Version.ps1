<#
.SYNOPSIS
    Updates version numbers across the project based on the latest git tag.

.DESCRIPTION
    This script retrieves the latest git tag, strips any 'v' prefix, and updates
    the version number in:
    - package.json
    - Src/GhostDraw/GhostDraw.csproj

.PARAMETER DryRun
    If specified, shows what would be changed without making actual changes.

.PARAMETER Tag
    Optionally specify a tag to use instead of fetching the latest from git.

.EXAMPLE
    .\Update-Version.ps1
    Updates all version files with the latest git tag.

.EXAMPLE
    .\Update-Version.ps1 -DryRun
    Shows what would be updated without making changes.

.EXAMPLE
    .\Update-Version.ps1 -Tag "v1.2.3"
    Updates all version files with the specified tag.
#>

[CmdletBinding()]
param(
    [switch]$DryRun,
    [string]$Tag
)

$ErrorActionPreference = 'Stop'

# Get the repository root (parent of Scripts directory)
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Repository root: $RepoRoot" -ForegroundColor Cyan

# Function to get the latest git tag
function Get-LatestGitTag {
    try {
        # Fetch latest tags from remote
        Write-Host "Fetching latest tags from remote..." -ForegroundColor Yellow
        git fetch --tags 2>&1 | Out-Null
        
        # Get the latest tag (sorted by version)
        $latestTag = git describe --tags --abbrev=0 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "No git tags found in repository"
        }
        
        return $latestTag.Trim()
    }
    catch {
        throw "Failed to get latest git tag: $_"
    }
}

# Function to extract version from tag (strips 'v' prefix if present)
function Get-VersionFromTag {
    param([string]$TagName)
    
    $version = $TagName
    
    # Strip 'v' or 'V' prefix if present
    if ($version -match '^[vV](.+)$') {
        $version = $Matches[1]
    }
    
    # Validate version format (should be semver-like: X.Y.Z or X.Y.Z-suffix)
    if ($version -notmatch '^\d+\.\d+(\.\d+)?(-[\w\d\.]+)?$') {
        throw "Invalid version format: '$version'. Expected format like '1.0.0' or '1.0.0-beta'"
    }
    
    return $version
}

# Function to update package.json
function Update-PackageJson {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return $false
    }
    
    $content = Get-Content $FilePath -Raw
    $json = $content | ConvertFrom-Json
    $oldVersion = $json.version
    
    if ($oldVersion -eq $NewVersion) {
        Write-Host "  package.json: Already at version $NewVersion" -ForegroundColor Green
        return $true
    }
    
    if ($DryRun) {
        Write-Host "  package.json: Would update $oldVersion -> $NewVersion" -ForegroundColor Yellow
        return $true
    }
    
    # Update version using regex to preserve formatting
    $updatedContent = $content -replace '"version"\s*:\s*"[^"]*"', "`"version`": `"$NewVersion`""
    
    Set-Content -Path $FilePath -Value $updatedContent -NoNewline
    Write-Host "  package.json: Updated $oldVersion -> $NewVersion" -ForegroundColor Green
    return $true
}

# Function to update .csproj file
function Update-CsprojFile {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return $false
    }
    
    $content = Get-Content $FilePath -Raw
    $fileName = Split-Path $FilePath -Leaf
    
    # Check if Version element exists
    if ($content -notmatch '<Version>([^<]*)</Version>') {
        Write-Warning "  $fileName`: No <Version> element found"
        return $false
    }
    
    $oldVersion = $Matches[1]
    
    if ($oldVersion -eq $NewVersion) {
        Write-Host "  $fileName`: Already at version $NewVersion" -ForegroundColor Green
        return $true
    }
    
    if ($DryRun) {
        Write-Host "  $fileName`: Would update $oldVersion -> $NewVersion" -ForegroundColor Yellow
        return $true
    }
    
    # Update version
    $updatedContent = $content -replace '<Version>[^<]*</Version>', "<Version>$NewVersion</Version>"
    
    Set-Content -Path $FilePath -Value $updatedContent -NoNewline
    Write-Host "  $fileName`: Updated $oldVersion -> $NewVersion" -ForegroundColor Green
    return $true
}

# Main execution
try {
    Write-Host ""
    Write-Host "=== GhostDraw Version Update Script ===" -ForegroundColor Magenta
    Write-Host ""
    
    # Get the tag to use
    if ($Tag) {
        $gitTag = $Tag
        Write-Host "Using specified tag: $gitTag" -ForegroundColor Cyan
    }
    else {
        $gitTag = Get-LatestGitTag
        Write-Host "Latest git tag: $gitTag" -ForegroundColor Cyan
    }
    
    # Extract version from tag
    $version = Get-VersionFromTag -TagName $gitTag
    Write-Host "Version to apply: $version" -ForegroundColor Cyan
    Write-Host ""
    
    if ($DryRun) {
        Write-Host "[DRY RUN] No files will be modified" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Define files to update
    $filesToUpdate = @(
        @{
            Path = Join-Path $RepoRoot "package.json"
            Type = "PackageJson"
        },
        @{
            Path = Join-Path $RepoRoot "Src\GhostDraw\GhostDraw.csproj"
            Type = "Csproj"
        }
    )
    
    Write-Host "Updating version files:" -ForegroundColor White
    
    $success = $true
    foreach ($file in $filesToUpdate) {
        switch ($file.Type) {
            "PackageJson" {
                $result = Update-PackageJson -FilePath $file.Path -NewVersion $version -DryRun:$DryRun
            }
            "Csproj" {
                $result = Update-CsprojFile -FilePath $file.Path -NewVersion $version -DryRun:$DryRun
            }
        }
        if (-not $result) {
            $success = $false
        }
    }
    
    Write-Host ""
    
    if ($success) {
        if ($DryRun) {
            Write-Host "Dry run complete. Run without -DryRun to apply changes." -ForegroundColor Yellow
        }
        else {
            Write-Host "Version update complete!" -ForegroundColor Green
        }
        exit 0
    }
    else {
        Write-Host "Version update completed with warnings." -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}
