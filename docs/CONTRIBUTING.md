# Contributing to GhostDraw

Thanks for your interest in contributing to **GhostDraw**! 🎉

This document explains how to propose changes, what we expect from contributions, and some project-specific guidelines (especially around safety, since GhostDraw installs global hooks and overlays your desktop).

---

## 1. Ways to Contribute

- 🐛 **Report bugs** via [GitHub Issues](https://github.com/RuntimeRascal/ghost-draw/issues)
- 💡 **Request features** (new tools, usability improvements, settings, etc.)
- 💻 **Code contributions** (bug fixes, features, refactors)
- 🧪 **Tests** (unit tests, edge-case coverage)
- 📝 **Docs** (README, `docs/`, in-app help)

Before starting larger work, please:
- Check existing **issues** and **PRs** to avoid duplicates
- Comment on an issue you want to tackle, or open a new one describing your proposal

---

## 2. Safety First (Critical for This Project)

GhostDraw uses **global keyboard and mouse hooks** and displays a fullscreen transparent overlay. This means bugs can affect basic system interaction.

Please follow these *non‑negotiable* rules:

1. **Never block the hook chain**
   - Hook callbacks must always call `CallNextHookEx`.
   - Return quickly (< 5 ms) and avoid heavy work in hooks.

2. **Handle all exceptions defensively**
   - Wrap critical paths (hooks, overlay activation, settings load/save) in `try/catch`.
   - Log exceptions; never let them crash the process without cleanup.

3. **Overlay must never trap the user**
   - `ESC` must always provide a way to escape drawing mode.
   - On unhandled exceptions, hide the overlay and release hooks.

4. **Always clean up on exit**
   - Unhook global hooks.
   - Dispose timers, event handlers, and unmanaged resources.

5. **Log appropriately**
   - Use `ILogger<T>` and Serilog.
   - Info/Debug for normal flow; Error/Critical for failures.
   - Avoid logging on every mouse move—this can flood logs and impact performance.

If in doubt, prioritize **stability and escape paths** over features.

---

## 3. Development Setup

### Prerequisites

- **OS**: Windows 10 or later
- **.NET SDK**: .NET 8 SDK
- **IDE**: Visual Studio, Rider, or VS Code with C# extension

### Clone and Build

```powershell
# Clone
git clone https://github.com/RuntimeRascal/ghost-draw.git
cd ghost-draw

# Build app + tests
dotnet build

# Run tests
dotnet test
```

The main application project is in `Src/GhostDraw/`. Tests live in `Tests/GhostDraw.Tests/`.

---

## 4. Branch & PR Workflow

1. **Create an issue** (or pick an existing one)
   - Describe the bug/feature and scope clearly.
2. **Create a feature branch** from `main`:

   ```powershell
   git checkout main
   git pull
   git checkout -b my-feature-branch
   ```

3. **Implement your changes**
   - Keep changes focused on the issue.
   - Avoid unrelated refactors in the same PR.

4. **Add / Update tests**
   - New features should have test coverage.
   - Bug fixes should include regression tests when feasible.

5. **Run tests locally** before opening a PR:

   ```powershell
   dotnet test
   ```

6. **Open a Pull Request**
   - Reference the issue (e.g., `Fixes #123`).
   - Summarize the change and any risks.
   - Mention anything that needs extra validation (installer, multi‑monitor, high DPI, etc.).

### 4.1 Alternative (Copilot-Powered) Workflow ☕

If you’d rather spend more time sipping coffee than wiring up boilerplate:

1. **Create a detailed issue** describing the bug/feature
   - Include context, expected behavior, edge cases, and screenshots if helpful.
2. **Assign a GitHub Copilot agent** to the issue
   - Let the agent spin up a branch, implement the changes, and open a PR for you.
3. **Go get a cup of coffee** (or two ☕☕)
4. **Come back and review the PR**
   - Run tests locally
   - Double-check safety guidelines (hooks, overlay behavior, ESC escape paths)
   - Request tweaks or follow-ups as needed

---

## 5. Coding Guidelines

### General

- Target **.NET 8**.
- Prefer **explicit namespaces** over ambiguous type usages when WPF/WinForms overlap (`Point`, `Color`, etc.).
- Use **dependency injection** for services (`ILogger<T>`, settings services, etc.).
- Avoid static state unless it is truly global and safe.

### WPF / UI

- Keep UI logic in `Views/` and behavior/logic in code‑behind or view models as appropriate.
- For icons in XAML:
  - Use **Segoe MDL2 Assets** (`FontFamily="Segoe MDL2 Assets"`).
  - Use hex codes like `&#xE76C;` for chevrons, checkmarks, etc.
  - Avoid raw Unicode emojis inside WPF controls; they may render as `?` on some systems.

### Hooks & Input

- Never perform heavy work directly in hook callbacks.
- Use events or dispatch to the UI thread for anything non‑trivial.
- Always ensure `CallNextHookEx` (or equivalent) is called.

### Logging

- Use structured logging with message templates:

  ```csharp
  _logger.LogInformation("Drawing mode {State}", enabled);
  _logger.LogError(ex, "Failed to load settings");
  ```

- Do not log every mouse move or wheel event at Info level—use Trace/Debug sparingly.

---

## 6. Testing Guidelines

When changing behavior, especially around input or overlay behavior, please:

- Add/extend unit tests in `Tests/GhostDraw.Tests/`.
- Favor fast, deterministic tests (no real hooks or UI interaction where possible).
- For higher‑risk changes, perform manual checks:
  - Drawing on multiple monitors
  - High DPI scaling
  - Rapid hotkey toggling
  - Using ESC to recover from mistakes

Example test command:

```powershell
dotnet test Tests/GhostDraw.Tests/GhostDraw.Tests.csproj
```

---

## 7. Installer Changes

If you modify the WiX installer (`Installer/`):

- Ensure user settings in `%LOCALAPPDATA%/GhostDraw` are **not** deleted on uninstall.
- Keep ICE warnings under control (or explicitly suppressed with rationale).
- Test install, upgrade, and uninstall flows on a real Windows machine.

---

## 8. Documentation

For user‑visible changes, update:

- `README.md` – high‑level features and screenshots
- `CHANGELOG.md` – versioned history (follow existing format)
- `docs/` – detailed behavior docs (e.g., key legend, feature plans)
- F1 help overlay – if you add or change shortcuts/tools

---

## 9. Code Review Expectations

PRs are more likely to be merged quickly if they:

- Are **small and focused**
- Include tests and docs updates
- Explain **why** the change is needed, not just what it does
- Call out any known limitations or follow‑up work

We may ask for changes to maintain safety, consistency, or clarity.

---

## 10. Questions?

If you’re unsure about anything:

- Open a **discussion** or **issue** on GitHub with your questions.
- Propose your approach before implementing large or risky changes.

Thanks again for helping make GhostDraw better! 👻💜
