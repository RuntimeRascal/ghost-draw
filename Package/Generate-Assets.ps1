# Generate MSIX visual assets from existing icon
# This script uses .NET System.Drawing to resize the icon to required sizes

param(
    [string]$SourceImage = "..\Src\GhostDraw\Assets\ghost-draw-icon.png",
    [string]$SplashSourceImage = "..\docs\hero-orig.png",
    [string]$OutputDir = "Images"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Generating MSIX Visual Assets" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Load System.Drawing assembly
Add-Type -AssemblyName System.Drawing

# Load source image
Write-Host "Loading source image: $SourceImage" -ForegroundColor Yellow
$sourceImg = [System.Drawing.Image]::FromFile((Resolve-Path $SourceImage))
Write-Host "Source image size: $($sourceImg.Width)x$($sourceImg.Height)" -ForegroundColor Green

$splashSourceImg = $null
if (Test-Path $SplashSourceImage) {
    Write-Host "Loading splash source image: $SplashSourceImage" -ForegroundColor Yellow
    $splashSourceImg = [System.Drawing.Image]::FromFile((Resolve-Path $SplashSourceImage))
    Write-Host "Splash source size: $($splashSourceImg.Width)x$($splashSourceImg.Height)" -ForegroundColor Green
} else {
    Write-Host "Splash source not found; falling back to default source" -ForegroundColor DarkYellow
}
Write-Host ""

# Define required assets
$assets = @(
    @{Name="Square44x44Logo.png"; Width=44; Height=44; Description="App list icon"},
    @{Name="Square150x150Logo.png"; Width=150; Height=150; Description="Medium tile"},
    @{Name="Square71x71Logo.png"; Width=71; Height=71; Description="Small tile"},
    @{Name="Wide310x150Logo.png"; Width=310; Height=150; Description="Wide tile"},
    @{Name="LargeTile.png"; Width=310; Height=310; Description="Large tile"},
    @{Name="StoreLogo.png"; Width=50; Height=50; Description="Store listing"},
    @{Name="SplashScreen.png"; Width=620; Height=300; Description="Launch splash"}
)

# Generate each asset
$count = 0
foreach ($asset in $assets) {
    $count++
    Write-Host "[$count/$($assets.Count)] Generating $($asset.Name) ($($asset.Width)x$($asset.Height)) - $($asset.Description)" -ForegroundColor Yellow

    $outputPath = Join-Path $OutputDir $asset.Name

    # Choose image source per asset
    $assetSource = $sourceImg
    $useCoverFill = $false
    if ($asset.Name -eq "SplashScreen.png" -and $splashSourceImg) {
        $assetSource = $splashSourceImg
        $useCoverFill = $true
    }

    # Create new bitmap with target size
    $newImg = New-Object System.Drawing.Bitmap($asset.Width, $asset.Height)

    # Create graphics object
    $graphics = [System.Drawing.Graphics]::FromImage($newImg)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Clear with transparent background
    $graphics.Clear([System.Drawing.Color]::Transparent)

    # Calculate scaling to fit while maintaining aspect ratio
    $srcAspect = $assetSource.Width / $assetSource.Height
    $dstAspect = $asset.Width / $asset.Height

    if ($useCoverFill) {
        # Cover the destination without letterboxing (crop overflow)
        if ($srcAspect -gt $dstAspect) {
            $scaledHeight = $asset.Height
            $scaledWidth = [int]($asset.Height * $srcAspect)
            $x = [int](($asset.Width - $scaledWidth) / 2)
            $y = 0
        } else {
            $scaledWidth = $asset.Width
            $scaledHeight = [int]($asset.Width / $srcAspect)
            $x = 0
            $y = [int](($asset.Height - $scaledHeight) / 2)
        }
    } else {
        # Fit within destination while preserving aspect ratio (letterbox)
        if ($srcAspect -gt $dstAspect) {
            # Source is wider - fit to width
            $scaledWidth = $asset.Width
            $scaledHeight = [int]($asset.Width / $srcAspect)
            $x = 0
            $y = [int](($asset.Height - $scaledHeight) / 2)
        } else {
            # Source is taller - fit to height
            $scaledHeight = $asset.Height
            $scaledWidth = [int]($asset.Height * $srcAspect)
            $x = [int](($asset.Width - $scaledWidth) / 2)
            $y = 0
        }
    }

    # Draw resized image
    $graphics.DrawImage($assetSource, $x, $y, $scaledWidth, $scaledHeight)

    # Save as PNG
    $newImg.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

    # Cleanup
    $graphics.Dispose()
    $newImg.Dispose()

    $fileSize = (Get-Item $outputPath).Length
    Write-Host "  Created: $outputPath ($([math]::Round($fileSize/1KB, 1)) KB)" -ForegroundColor Green
}

# Cleanup
$sourceImg.Dispose()
if ($splashSourceImg) {
    $splashSourceImg.Dispose()
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Asset Generation Complete! " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Generated $($assets.Count) assets in $OutputDir/" -ForegroundColor White
Write-Host ""
Write-Host "Note: These are automatically generated placeholders." -ForegroundColor Yellow
Write-Host "For production, consider creating custom designs for:" -ForegroundColor Yellow
Write-Host "  - Wide310x150Logo.png (wide tile with app name/branding)" -ForegroundColor Gray
Write-Host "  - SplashScreen.png (620x300 with centered logo and tagline)" -ForegroundColor Gray
