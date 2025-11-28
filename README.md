# GhostDraw

A lightweight Windows desktop application that allows you to draw directly on your screen using a simple keyboard hotkey and mouse input.

## Features

- **Global Hotkey**: Toggle drawing mode with `Ctrl+Alt+D`
- **Fullscreen Overlay**: Draw on top of any application
- **System Tray Integration**: Runs quietly in the background
- **Emergency Exit**: Press `ESC` to quickly hide the overlay
- **Transparent Drawing**: Overlay is completely transparent when not actively drawing

## Requirements

- Windows 10 or later
- .NET 8 Runtime

## Installation

1. Download the latest release from the [Releases](https://github.com/RuntimeRascal/ghost-draw/releases) page
2. Extract the ZIP file to a folder of your choice
3. Run `GhostDraw.exe`
4. The application will start minimized to the system tray

## Usage

1. **Start Drawing**: Press `Ctrl+Alt+D` to activate the drawing overlay
2. **Draw**: Click and drag with your mouse to draw on the screen
3. **Exit Drawing Mode**: Press `Ctrl+Alt+D` again or press `ESC`
4. **Exit Application**: Right-click the system tray icon and select "Exit"

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) or any .NET-compatible IDE

### Build Steps

```bash
# Clone the repository
git clone https://github.com/RuntimeRascal/ghost-draw.git
cd ghost-draw

# Navigate to the source directory
cd src

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --configuration Release
```

## Architecture

GhostDraw is built using modern .NET practices:

- **WPF** - UI framework and overlay rendering
- **Global Windows Hooks** - Keyboard and mouse input capture
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Structured Logging** - Serilog + Microsoft.Extensions.Logging
- **.NET 8** - Latest LTS framework

### Project Structure

```
ghost-draw/
??? src/                    # Main application source code
?   ??? GhostDraw.csproj   # Project file
?   ??? ...                # Application code
??? tests/                 # Unit and integration tests
??? docs/                  # Additional documentation
??? .github/               # GitHub configuration
?   ??? copilot-instructions.md  # AI assistant guidelines
??? README.md              # This file
```

## Safety & Stability

GhostDraw intercepts global keyboard and mouse input. The application is designed with safety as the top priority:

- **Fail-Safe Design**: Crashes won't lock you out of your system
- **Emergency Exit**: ESC key always hides the overlay
- **Fast Hook Processing**: All input hooks complete in < 5ms
- **Graceful Error Handling**: All critical paths are protected with try-catch blocks
- **Proper Cleanup**: Resources are always released on exit

## Contributing

Contributions are welcome! Please read the [Copilot Instructions](.github/copilot-instructions.md) for detailed guidelines on:

- Safety requirements
- Code style and conventions
- Architecture patterns
- Testing considerations

### Development Guidelines

1. **Safety First**: Never compromise user system stability
2. **Test Thoroughly**: Especially edge cases (multi-monitor, high DPI, rapid input)
3. **Log Appropriately**: Use structured logging with proper levels
4. **Handle Errors**: Catch and log exceptions gracefully
5. **Keep It Fast**: Hook callbacks must be lightning fast (< 5ms)

## Roadmap

Future features under consideration:

- [ ] Brush customization (color, thickness, opacity)
- [ ] Stroke persistence (save/load drawings)
- [ ] Undo/redo functionality
- [ ] Screenshot integration
- [ ] Multiple drawing tools (pen, highlighter, eraser)
- [ ] Shape tools (line, rectangle, circle)

## Known Issues

- None currently reported

## License

[Specify your license here - e.g., MIT, GPL, etc.]

## Acknowledgments

Built with:
- [WPF](https://github.com/dotnet/wpf) - UI framework
- [Serilog](https://serilog.net/) - Logging library
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) - DI container

## Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/RuntimeRascal/ghost-draw/issues) page
2. Create a new issue with detailed information
3. Include logs from `%LOCALAPPDATA%\GhostDraw\logs\` if applicable

---

**?? Important**: This application uses global keyboard and mouse hooks. Use responsibly and ensure you understand the [safety guidelines](.github/copilot-instructions.md) if modifying the code.
