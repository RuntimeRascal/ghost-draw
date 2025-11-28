# PowerShell script to read and display the settings.json file
$settingsPath = "$env:LOCALAPPDATA\GhostDraw\settings.json"

Write-Host "Settings file location: $settingsPath" -ForegroundColor Cyan

if (Test-Path $settingsPath) {
    Write-Host "`nFile exists! Contents:" -ForegroundColor Green
    $content = Get-Content $settingsPath -Raw
    Write-Host $content
    
    Write-Host "`nParsed JSON:" -ForegroundColor Yellow
    $json = $content | ConvertFrom-Json
    Write-Host "MinBrushThickness: $($json.minBrushThickness) (Type: $($json.minBrushThickness.GetType().Name))"
    Write-Host "MaxBrushThickness: $($json.maxBrushThickness) (Type: $($json.maxBrushThickness.GetType().Name))"
    Write-Host "BrushThickness: $($json.brushThickness) (Type: $($json.brushThickness.GetType().Name))"
} else {
    Write-Host "`nFile does NOT exist!" -ForegroundColor Red
    Write-Host "Create settings by running the app and changing min/max values."
}
