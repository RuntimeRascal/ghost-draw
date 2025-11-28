# GhostDraw Tests

This directory contains unit tests for the GhostDraw application.

## Test Projects

- **GhostDraw.Tests** - Main test project containing unit tests for GhostDraw functionality

## Test Statistics

? **Total Tests**: 35  
? **All Passing**: 35/35  
?? **Execution Time**: ~93ms  

## Requirements

- **.NET 8 SDK** with Windows desktop support
- **Target Framework**: `net8.0-windows` (required because GhostDraw is a WPF application)

## Test Coverage

### AppSettingsTests (11 tests)
- Default values and initialization
- Color palette validation
- Clone functionality (deep copy)
- Property validation (parameterized tests)

### AppSettingsServiceTests (24 tests)
- Settings persistence and state management
- Brush color setting and cycling
- Brush thickness with clamping
- Hotkey configuration
- Lock mode settings
- Range validation
- Multi-operation consistency

See [TEST_COVERAGE_REPORT.md](TEST_COVERAGE_REPORT.md) for detailed coverage analysis.

## Test Isolation

?? **Important**: Tests automatically clean up settings files to ensure isolation.

The `AppSettingsServiceTests` class includes cleanup logic that:
- Deletes settings files **before** tests run (clean slate)
- Deletes settings files **after** tests run (no artifacts)

This prevents test pollution where settings from one test run affect subsequent runs.

See [TEST_FAILURE_FIX.md](TEST_FAILURE_FIX.md) for details about the settings persistence issue and solution.

## Running Tests

### From Command Line

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AppSettingsTests"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### From Visual Studio

1. Open the solution in Visual Studio
2. Open **Test Explorer** (Test > Test Explorer)
3. Click "Run All" to run all tests
4. Or right-click specific tests to run individually

## Troubleshooting

### Tests Failing with Unexpected Values?

If tests fail with unexpected brush colors or thickness values:

1. **Manually delete the settings file**:
   ```powershell
   $settingsPath = Join-Path $env:LOCALAPPDATA "GhostDraw\settings.json"
   if (Test-Path $settingsPath) { Remove-Item $settingsPath -Force }
   ```

2. **Run tests again**:
   ```bash
   dotnet test
   ```

The cleanup code in tests should handle this automatically, but manual cleanup may be needed if:
- Tests were interrupted/crashed
- File was locked by another process
- Permissions prevent deletion

### Settings File Location

The actual settings file used by the application:
```
%LOCALAPPDATA%\GhostDraw\settings.json
```

Tests clean this up automatically to prevent pollution.

## Test Structure

### Test Categories

- **AppSettingsTests** - Tests for `AppSettings` model
  - Default values
  - Property validation
  - Clone functionality
  - Color palette management

- **AppSettingsServiceTests** - Tests for `AppSettingsService`
  - Settings persistence
  - Color cycling
  - Brush thickness clamping
  - Hotkey configuration
  - Lock mode settings

## Test Frameworks & Tools

- **xUnit** - Testing framework
- **Moq** - Mocking library for isolating dependencies
- **.NET 8** - Target framework

## Adding New Tests

When adding new tests:

1. Follow the naming convention: `[ClassName]Tests.cs`
2. Use xUnit `[Fact]` for single test cases
3. Use xUnit `[Theory]` with `[InlineData]` for parameterized tests
4. Use descriptive test method names: `Method_Scenario_ExpectedResult`
5. Follow Arrange-Act-Assert pattern
6. Add XML comments for complex test scenarios

Example:

```csharp
[Theory]
[InlineData("#FF0000", "Red")]
[InlineData("#00FF00", "Green")]
public void SetBrushColor_ShouldAcceptValidHexColors(string hexColor, string colorName)
{
    // Arrange
    var service = new AppSettingsService(mockLogger.Object);
    
    // Act
    service.SetBrushColor(hexColor);
    
    // Assert
    Assert.Equal(hexColor, service.CurrentSettings.BrushColor);
}
```

## Test Coverage Goals

- **Unit Tests**: Aim for >80% code coverage
- **Critical Paths**: 100% coverage for safety-critical code (hooks, overlay management)
- **Edge Cases**: Test boundary conditions, null handling, and error scenarios

## Future Test Additions

As features are added, tests should cover:

- Drawing mode lock behavior
- Hotkey detection and handling
- Color cycling edge cases
- Cursor generation with different colors
- Settings persistence across sessions
- Error handling and recovery
- Multi-monitor scenarios
- High DPI scaling

## Notes

- Some tests are marked with placeholder assertions indicating refactoring needs
- `AppSettingsService` may benefit from dependency injection refactoring to make file I/O testable
- Consider adding integration tests for end-to-end scenarios
- Mock heavy dependencies (file system, Windows APIs) for unit test isolation
