# GitHub Workflows

This directory contains GitHub Actions workflows for building and releasing GhostDraw.

## Workflows

### 1. CI Build (`ci.yml`)

**Triggers:**
- Push to `main` branch
- Pull requests to `main` branch

**What it does:**
- Builds the application
- Runs tests
- Builds the installer MSI
- Uploads installer as artifact (retained for 7 days)

**Purpose:** Ensures the code builds successfully and the installer can be created.

---

### 2. Release Build (`release.yml`)

**Triggers:**
- Push of version tags (e.g., `v1.0.0`, `v1.2.3`)

**What it does:**
1. Extracts version from the tag
2. Builds the application with the version number
3. Generates WiX component list
4. Builds the installer MSI
5. Creates a GitHub release
6. Uploads the MSI to the release

**Purpose:** Automates the release process and creates downloadable installers.

---

## Creating a Release

To create a new release:

### 1. Update Version (Optional)

If you want to update version strings in the code:

```bash
# Update version in Product.wxs if needed
# Update version in GhostDraw.csproj if needed
```

### 2. Create and Push a Version Tag

```bash
# Create a tag
git tag v1.0.0

# Or create an annotated tag with a message
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push the tag to GitHub
git push origin v1.0.0
```

### 3. Monitor the Workflow

1. Go to **Actions** tab in GitHub
2. Watch the "Build and Release" workflow run
3. Once complete, check the **Releases** page

### 4. Verify the Release

The release will include:
- Release notes
- `GhostDrawSetup-1.0.0.msi` installer file
- Download statistics

---

## Version Numbering

Use [Semantic Versioning](https://semver.org/):
- **MAJOR.MINOR.PATCH** (e.g., `v1.0.0`)
- **MAJOR**: Breaking changes
- **MINOR**: New features (backwards compatible)
- **PATCH**: Bug fixes

Examples:
- `v1.0.0` - Initial release
- `v1.1.0` - New feature added
- `v1.1.1` - Bug fix
- `v2.0.0` - Breaking change

---

## Pre-release Versions

To create a pre-release:

```bash
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

Then manually mark it as "pre-release" in the GitHub UI.

---

## Troubleshooting

### Workflow Failed

1. Check the Actions tab for error logs
2. Common issues:
   - Missing dependencies
   - Test failures
   - WiX compilation errors

### Release Not Created

1. Ensure the tag starts with `v` (e.g., `v1.0.0` not `1.0.0`)
2. Check the tag was pushed to GitHub: `git push origin <tag>`
3. Verify workflow permissions in repository settings

### Installer Not Uploaded

1. Check that the MSI file path is correct
2. Verify the version in the filename matches the tag
3. Check workflow logs for upload errors

---

## Manual Build

To build locally without creating a release:

```powershell
# From the repository root
cd installer
.\build.ps1 -Version "1.0.0"
```

The installer will be at:
```
installer\bin\x64\Release\GhostDrawSetup-1.0.0.msi
```
