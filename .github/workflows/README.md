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

## Version Numbering

Use 4-part version numbers (Major.Minor.Build.Revision):
- Example initial store release: `v2.0.0.0`
- Increment the Revision (fourth part) for servicing updates unless a larger change warrants bumping Build/Minor/Major.

---

## Troubleshooting

### Workflow Failed

1. Check the Actions tab for error logs
2. Common issues:
   - Missing dependencies
   - Test failures
   - WiX compilation errors

### Release Not Created

1. Ensure the tag starts with `v` (e.g., `v1.0.0.0` not `1.0.0.0`)
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
.\build.ps1 -Version "1.0.0.0"
```

The installer will be at:
```
installer\bin\x64\Release\GhostDrawSetup-1.0.0.0.msi
```
