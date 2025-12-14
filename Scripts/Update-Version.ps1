<#
.SYNOPSIS
    Manages version numbers across the project - displays, bumps, updates files, and creates git tags.

.DESCRIPTION
    This script provides flexible version management:
    - Fetches and displays the latest git tag version
    - Optionally bumps the version (revision/build/minor/major) using 4-part versions
    - Optionally updates version in project files (package.json, .csproj, .wixproj, appxmanifest)
    - Optionally creates and pushes git tags

    Files that can be updated:
    - package.json
    - Src/GhostDraw/GhostDraw.csproj
    - Installer/GhostDraw.Installer.wixproj
    - Package/Package.appxmanifest

.PARAMETER DryRun
    If specified, shows what would be changed without making any modifications.

.PARAMETER Tag
    Optional. Use a specific version/tag instead of fetching the latest from git.
    Can be specified with or without 'v' prefix (e.g., "v1.2.3" or "1.2.3").

.PARAMETER Bump
    Optional. Increment the version (patch, minor, or major).
    - If -Tag is provided, bumps that version
    - Otherwise, bumps the latest git tag version

.PARAMETER UpdateProjFiles
    Optional. If specified, updates the version in all project files.

.PARAMETER CreateTag
    Optional. If specified, creates a git tag with the resolved version.

.PARAMETER PushTag
    Optional. If specified along with -CreateTag, pushes the tag to origin.

.EXAMPLE
    .\Update-Version.ps1
    Displays the latest git tag version (no changes made).

.EXAMPLE
    .\Update-Version.ps1 -Tag "1.2.3"
    Displays version 1.2.3 (no changes made).

.EXAMPLE
    .\Update-Version.ps1 -Bump patch
    Displays what the next patch version would be (e.g., 1.0.2 -> 1.0.3).

.EXAMPLE
    .\Update-Version.ps1 -Bump minor -UpdateProjFiles
    Bumps minor version and updates all project files.

.EXAMPLE
    .\Update-Version.ps1 -Tag "2.0.0" -UpdateProjFiles
    Sets version 2.0.0 in all project files.

.EXAMPLE
    .\Update-Version.ps1 -Bump major -CreateTag -PushTag
    Bumps major version and creates/pushes a git tag (doesn't update files).

.EXAMPLE
    .\Update-Version.ps1 -Bump minor -UpdateProjFiles -CreateTag -PushTag
    Bumps minor version, updates all files, creates tag, and pushes it.

.EXAMPLE
    .\Update-Version.ps1 -Bump patch -UpdateProjFiles -CreateTag -PushTag -DryRun
    Shows what would happen without making any changes.
#>

param(
    [switch]$DryRun,
    [string]$Tag,
    [ValidateSet("revision", "patch", "build", "minor", "major")]
    [string]$Bump,
    [switch]$UpdateProjFiles,
    [switch]$CreateTag,
    [switch]$PushTag
)

$ErrorActionPreference = "Stop"

# Get the repository root (parent of Scripts directory)
$repoRoot = Split-Path -Parent $PSScriptRoot

# Files to update (relative to repo root)
$filesToUpdate = @(
    "package.json",
    "Src/GhostDraw/GhostDraw.csproj",
    "Installer/GhostDraw.Installer.wixproj",
    "Package/Package.appxmanifest"
)

#region Helper Functions

function Get-LatestGitTag {
    # Fetch tags from remote
    Write-Host "Fetching tags from remote..." -ForegroundColor Gray
    git fetch --tags 2>$null
    
    # Get the latest tag sorted by version
    $latestTag = git tag -l --sort=-v:refname | Select-Object -First 1
    
    if (-not $latestTag) {
        throw "No git tags found. Please create a tag first (e.g., git tag v1.0.0)"
    }
    
    return $latestTag
}

function Get-VersionFromTag {
    param([string]$TagName)
    
    # Strip 'v' prefix if present
    $version = $TagName -replace "^v", ""
    
    # Validate version format (basic semver)
    if ($version -notmatch "^\d+\.\d+\.\d+(\.\d+)?$") {
        throw "Tag '$TagName' does not appear to be a valid 3- or 4-part version"
    }

    # Normalize to 4-part version (Major.Minor.Build.Revision)
    if ($version -notmatch "^\d+\.\d+\.\d+\.\d+$") {
        $version = "$version.0"
    }

    return $version
}

function Get-FourPartVersion {
    param([string]$Version)

    if ($Version -notmatch "^\d+\.\d+\.\d+(\.\d+)?$") {
        throw "Version must be 3- or 4-part (Major.Minor.Build.Revision)"
    }

    if ($Version -notmatch "^\d+\.\d+\.\d+\.\d+$") {
        return "$Version.0"
    }

    return $Version
}

function Get-BumpedVersion {
    param(
        [string]$CurrentVersion,
        [string]$BumpType
    )
    
    $parts = $CurrentVersion -split "\."
    # Ensure 4 parts
    while ($parts.Count -lt 4) { $parts += 0 }

    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $build = [int]$parts[2]
    $revision = [int]$parts[3]
    
    switch ($BumpType) {
        "major" {
            $major++
            $minor = 0
            $build = 0
            $revision = 0
        }
        "minor" {
            $minor++
            $build = 0
            $revision = 0
        }
        "build" {
            $build++
            $revision = 0
        }
        "patch" { # backward compat maps to revision
            $revision++
        }
        "revision" {
            $revision++
        }
    }
    
    return "$major.$minor.$build.$revision"
}

function New-GitTag {
    param(
        [string]$Version,
        [switch]$Push,
        [switch]$DryRun
    )
    
    $tagName = "v$Version"
    
    if ($DryRun) {
        Write-Host "  [DRY-RUN] Would create git tag: $tagName" -ForegroundColor Cyan
        if ($Push) {
            Write-Host "  [DRY-RUN] Would push tag to origin" -ForegroundColor Cyan
        }
        return
    }
    
    # Check if tag already exists
    $existingTag = git tag -l $tagName
    if ($existingTag) {
        throw "Tag '$tagName' already exists. Cannot create duplicate tag."
    }
    
    # Create the tag
    Write-Host "  Creating git tag: $tagName" -ForegroundColor Yellow
    git tag $tagName
    Write-Host "  Tag created: $tagName" -ForegroundColor Green
    
    if ($Push) {
        Write-Host "  Pushing tag to origin..." -ForegroundColor Yellow
        git push origin $tagName
        Write-Host "  Tag pushed successfully" -ForegroundColor Green
    }
}

function Update-PackageJson {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )
    
    $content = Get-Content $FilePath -Raw
    $pattern = '"version":\s*"[^"]*"'
    $replacement = "`"version`": `"$NewVersion`""
    
    if ($content -match $pattern) {
        $currentVersion = [regex]::Match($content, '"version":\s*"([^"]*)"').Groups[1].Value
        
        if ($currentVersion -eq $NewVersion) {
            Write-Host "    Already at version $NewVersion" -ForegroundColor Gray
            return
        }
        
        if ($DryRun) {
            Write-Host "    [DRY-RUN] Would update: $currentVersion -> $NewVersion" -ForegroundColor Cyan
        } else {
            $newContent = $content -replace $pattern, $replacement
            Set-Content $FilePath -Value $newContent -NoNewline
            Write-Host "    Updated: $currentVersion -> $NewVersion" -ForegroundColor Green
        }
    } else {
        Write-Host "    Warning: Could not find version field" -ForegroundColor Yellow
    }
}

function Update-CsprojVersion {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )
    
    $content = Get-Content $FilePath -Raw
    $pattern = '<Version>([^<]*)</Version>'
    
    if ($content -match $pattern) {
        $currentVersion = [regex]::Match($content, $pattern).Groups[1].Value
        
        if ($currentVersion -eq $NewVersion) {
            Write-Host "    Already at version $NewVersion" -ForegroundColor Gray
            return
        }
        
        if ($DryRun) {
            Write-Host "    [DRY-RUN] Would update: $currentVersion -> $NewVersion" -ForegroundColor Cyan
        } else {
            $newContent = $content -replace $pattern, "<Version>$NewVersion</Version>"
            Set-Content $FilePath -Value $newContent -NoNewline
            Write-Host "    Updated: $currentVersion -> $NewVersion" -ForegroundColor Green
        }
    } else {
        Write-Host "    Warning: Could not find <Version> element" -ForegroundColor Yellow
    }
}

function Update-WixVersion {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )
    
    $content = Get-Content $FilePath -Raw
    # Match: <Version Condition="'$(Version)' == ''">X.Y.Z</Version>
    $pattern = '<Version\s+Condition="''\$\(Version\)''\s*==\s*''''">([^<]*)</Version>'
    
    if ($content -match $pattern) {
        $currentVersion = [regex]::Match($content, $pattern).Groups[1].Value
        
        if ($currentVersion -eq $NewVersion) {
            Write-Host "    Already at version $NewVersion" -ForegroundColor Gray
            return
        }
        
        if ($DryRun) {
            Write-Host "    [DRY-RUN] Would update: $currentVersion -> $NewVersion" -ForegroundColor Cyan
        } else {
            $replacement = "<Version Condition=`"'`$(Version)' == ''`">$NewVersion</Version>"
            $newContent = $content -replace $pattern, $replacement
            Set-Content $FilePath -Value $newContent -NoNewline
            Write-Host "    Updated: $currentVersion -> $NewVersion" -ForegroundColor Green
        }
    } else {
        Write-Host "    Warning: Could not find conditional <Version> element" -ForegroundColor Yellow
    }
}

function Update-AppxManifestVersion {
    param(
        [string]$FilePath,
        [string]$NewVersion,
        [switch]$DryRun
    )

    [xml]$manifest = Get-Content $FilePath
    $current = $manifest.Package.Identity.Version

    if ($current -eq $NewVersion) {
        Write-Host "    Already at version $NewVersion" -ForegroundColor Gray
        return
    }

    Write-Host "    Current: $current" -ForegroundColor Red
    Write-Host "    Expected: $NewVersion" -ForegroundColor Green

    if ($DryRun) {
        Write-Host "    [DRY-RUN] Would update appxmanifest" -ForegroundColor Cyan
    } else {
        $manifest.Package.Identity.Version = $NewVersion
        $manifest.Save($FilePath)
        Write-Host "    Updated" -ForegroundColor Green
    }
}

#endregion

#region Main Execution

Write-Host ""
Write-Host "=== Version Management Script ===" -ForegroundColor Magenta
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY-RUN MODE - No changes will be made]" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Determine the base version
if ($Tag) {
    $baseVersion = Get-VersionFromTag -TagName $Tag
    Write-Host "Specified version: $baseVersion" -ForegroundColor Cyan
} else {
    $latestTag = Get-LatestGitTag
    $baseVersion = Get-VersionFromTag -TagName $latestTag
    Write-Host "Latest git tag: $latestTag (version $baseVersion)" -ForegroundColor Cyan
}

# Step 2: Apply bump if requested
if ($Bump) {
    $targetVersion = Get-BumpedVersion -CurrentVersion $baseVersion -BumpType $Bump
    Write-Host "Bump type: $Bump" -ForegroundColor Cyan
    Write-Host "Bumped version: $baseVersion -> $targetVersion" -ForegroundColor Green
} else {
    $targetVersion = $baseVersion
}

Write-Host ""
Write-Host "Resolved version (Major.Minor.Build.Revision): $targetVersion" -ForegroundColor White
Write-Host ""

# Step 3: Update project files if requested
if ($UpdateProjFiles) {
    Write-Host "Updating project files:" -ForegroundColor White
    
    foreach ($file in $filesToUpdate) {
        $fullPath = Join-Path $repoRoot $file
        
        if (-not (Test-Path $fullPath)) {
            Write-Host "  File not found: $file" -ForegroundColor Red
            continue
        }
        
        Write-Host "  $file" -ForegroundColor Gray
        
        switch -Wildcard ($file) {
            "*.json" {
                Update-PackageJson -FilePath $fullPath -NewVersion $targetVersion -DryRun:$DryRun
            }
            "*.csproj" {
                Update-CsprojVersion -FilePath $fullPath -NewVersion $targetVersion -DryRun:$DryRun
            }
            "*.wixproj" {
                Update-WixVersion -FilePath $fullPath -NewVersion $targetVersion -DryRun:$DryRun
            }
            "*.appxmanifest" {
                $fourPart = Get-FourPartVersion -Version $targetVersion
                Update-AppxManifestVersion -FilePath $fullPath -NewVersion $fourPart -DryRun:$DryRun
            }
        }
    }
    Write-Host ""
} else {
    Write-Host "Project files: Not updated (use -UpdateProjFiles to update)" -ForegroundColor Gray
    Write-Host ""
}

# Step 4: Create git tag if requested
if ($CreateTag) {
    Write-Host "Git tag:" -ForegroundColor White
    New-GitTag -Version $targetVersion -Push:$PushTag -DryRun:$DryRun
    Write-Host ""
} else {
    Write-Host "Git tag: Not created (use -CreateTag to create)" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=== Complete ===" -ForegroundColor Magenta
Write-Host ""

#endregion
Write-Host ""