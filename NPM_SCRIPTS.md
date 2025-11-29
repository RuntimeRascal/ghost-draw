# NPM Scripts Documentation

This project uses npm scripts as a convenient interface for common development tasks. Below is a complete reference of all available scripts.

## Prerequisites

- Node.js 18+ and npm 9+
- .NET 8 SDK
- WiX Toolset 4.0.5 (for installer builds)

## Quick Start

```bash
# Install dependencies and build
npm run restore
npm run build

# Run tests
npm test

# Build installer
npm run installer
```

## Available Scripts

### Building

#### `npm run build`
Build the entire solution in Debug configuration.
```bash
npm run build
```

#### `npm run build:release`
Build the entire solution in Release configuration.
```bash
npm run build:release
```

#### `npm run restore`
Restore NuGet package dependencies for the solution.
```bash
npm run restore
```

### Testing

#### `npm test`
Run all unit tests with normal verbosity.
```bash
npm test
```

#### `npm run test:watch`
Run tests in watch mode - automatically re-runs tests when code changes.
```bash
npm run test:watch
```

### Publishing

#### `npm run publish`
Publish the application as a self-contained Windows x64 executable.
```bash
npm run publish
```

#### `npm run publish:ver`
Publish the application with a specific version.
```bash
# Windows CMD
set VER=1.2.3 && npm run publish:ver

# PowerShell
$env:VER="1.2.3"; npm run publish:ver

# Git Bash
VER=1.2.3 npm run publish:ver
```

### Installer

#### `npm run installer`
Build the complete installer (builds app, generates file list, creates MSI).
```bash
npm run installer
```

#### `npm run installer:ver`
Build the installer with a specific version.
```bash
# Windows CMD
set VER=1.2.3 && npm run installer:ver

# PowerShell
$env:VER="1.2.3"; npm run installer:ver

# Git Bash
VER=1.2.3 npm run installer:ver
```

#### `npm run installer:generate`
Generate the WiX component file list from the published application.
```bash
npm run installer:generate
```

### Release

#### `npm run release`
Complete release build: clean, test, and build installer.
```bash
npm run release
```

#### `npm run release:ver`
Complete release build with a specific version.
```bash
# Windows CMD
set VER=1.2.3 && npm run release:ver

# PowerShell
$env:VER="1.2.3"; npm run release:ver

# Git Bash
VER=1.2.3 npm run release:ver
```

### Development

#### `npm run dev`
Build and run the application in development mode.
```bash
npm run dev
```

#### `npm run dev:watch`
Run the application in watch mode - automatically rebuilds and restarts on code changes.
```bash
npm run dev:watch
```

### Code Formatting

#### `npm run format`
Format all code in the solution using dotnet-format.
```bash
npm run format
```

#### `npm run format:check`
Check if code formatting is correct without making changes.
```bash
npm run format:check
```

### Cleaning

#### `npm run clean`
Clean all build outputs and intermediate files.
```bash
npm run clean
```

#### `npm run clean:installer`
Clean only installer build outputs.
```bash
npm run clean:installer
```

### Version Bumping

#### `npm run version:bump:patch`
Bump the patch version (1.0.0 → 1.0.1).
```bash
npm run version:bump:patch
```

#### `npm run version:bump:minor`
Bump the minor version (1.0.0 → 1.1.0).
```bash
npm run version:bump:minor
```

#### `npm run version:bump:major`
Bump the major version (1.0.0 → 2.0.0).
```bash
npm run version:bump:major
```

## Common Workflows

### Development Workflow
```bash
# Start development with watch mode
npm run dev:watch

# In another terminal, run tests in watch mode
npm run test:watch
```

### Creating a Release
```bash
# Bump version
npm run version:bump:minor

# Get the new version from package.json
# Then build release with that version
set VER=1.2.0 && npm run release:ver

# Or use the current package.json version
npm run release
```

### CI/CD Integration
The npm scripts integrate seamlessly with the existing GitHub Actions workflows:
- `.github/workflows/ci.yml` - Runs on every push to main
- `.github/workflows/release.yml` - Runs on version tags (v*.*.*)

### Quick Build and Test
```bash
# Build everything and run tests
npm run build && npm test

# Or for release configuration
npm run build:release && npm test
```

## Environment Variables

### `VER`
Used by version-specific scripts to set the build version:
```bash
# Windows CMD
set VER=1.2.3

# PowerShell
$env:VER="1.2.3"

# Git Bash / Linux
export VER=1.2.3
```

## Notes

- All scripts run from the repository root directory
- Installer scripts require the application to be published first
- Version scripts use the `VER` environment variable
- The installer output is placed in `Installer/bin/x64/Release/`
