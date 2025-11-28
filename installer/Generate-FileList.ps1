# Generate-FileList.ps1
# Auto-generates WiX component list for all files in publish folder with proper directory structure

param(
    [string]$PublishPath = "..\src\bin\Release\net8.0-windows\win-x64\publish",
    [string]$OutputFile = "HarvestedFiles.wxs"
)

$files = Get-ChildItem -Path $PublishPath -Recurse -File
$publishPathResolved = (Resolve-Path $PublishPath).Path

# Track all unique directories and assign IDs
$dirIds = @{}
$dirIds[""] = "INSTALLFOLDER"  # Root directory

foreach ($file in $files) {
    $relativePath = $file.FullName.Replace($publishPathResolved + "\", "")
    $dir = [System.IO.Path]::GetDirectoryName($relativePath)

    if (![string]::IsNullOrEmpty($dir) -and !$dirIds.ContainsKey($dir)) {
        # Generate unique directory ID from path
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($dir)
        $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
        $hashString = [System.BitConverter]::ToString($hash).Replace("-", "").Substring(0, 16)
        $dirIds[$dir] = "Dir_" + $hashString
    }
}

# Start building the WiX XML
$wxs = @"
<?xml version="1.0" encoding="UTF-8"?>
<Include xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <DirectoryRef Id="INSTALLFOLDER">
"@

# Generate Directory elements for each subdirectory
foreach ($dir in ($dirIds.Keys | Where-Object { $_ -ne "" } | Sort-Object)) {
    $dirName = [System.IO.Path]::GetFileName($dir)
    $dirId = $dirIds[$dir]
    $wxs += @"

      <Directory Id="$dirId" Name="$dirName" />
"@
}

$wxs += @"

    </DirectoryRef>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ProductComponents">
"@

# Generate Component for each file
foreach ($file in $files) {
    $relativePath = $file.FullName.Replace($publishPathResolved + "\", "")
    $dir = [System.IO.Path]::GetDirectoryName($relativePath)
    $fileName = [System.IO.Path]::GetFileName($relativePath)

    # Get the directory ID for this file
    if ([string]::IsNullOrEmpty($dir)) {
        $dirId = "INSTALLFOLDER"
    } else {
        $dirId = $dirIds[$dir]
    }

    # Create unique IDs and GUID using hash of the full relative path
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($relativePath)
    $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    $hashString = [System.BitConverter]::ToString($hash).Replace("-", "")

    # Use first 16 chars for IDs
    $componentId = "Cmp_" + $hashString.Substring(0, 16)
    $fileId = "Fil_" + $hashString.Substring(0, 16)

    # Create GUID from hash (take 32 hex chars and format as GUID)
    $guidStr = $hashString.Substring(0, 32)
    $guid = "{$($guidStr.Substring(0,8))-$($guidStr.Substring(8,4))-$($guidStr.Substring(12,4))-$($guidStr.Substring(16,4))-$($guidStr.Substring(20,12))}"

    $wxs += @"

      <Component Id="$componentId" Guid="$guid" Directory="$dirId">
        <File Id="$fileId" Source="`$(var.PublishDir)\$relativePath" KeyPath="yes" />
      </Component>
"@
}

$wxs += @"

    </ComponentGroup>
  </Fragment>
</Include>
"@

$wxs | Out-File -FilePath $OutputFile -Encoding UTF8
Write-Host "Generated $($files.Count) file components in $OutputFile"
