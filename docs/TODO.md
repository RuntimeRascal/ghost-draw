# TODO List

- [ ] Add new setting to persist drawings. If enabled drawings will persist across draw mode activations. So if activate draw mode then draw on overlays then deactivate draw mode the drawings will persist when you re-activate draw mode again. Pretty much we don't clear drawings in this mode. We leave clearing or undoing drawings to the user.
- [ ] Screen save ctrl+s should prompt user for which screen to save. When ctrl+s is pressed drawing is temporarily disabled and a modal prompts user to select which screen to save or save all. Once user selects something, the modal closes then the chosen screen is saved. We have a delete key function that clears all drawings and this function opens a modal to prompt user while disabling drawing so that the user can interact with the modal. We will be doing something similar to this.

## Pre Win Store Deployment Fixes/Changes
- [X] Update default settings
	- [X] active brush default `#000000`
	- [X] Color palette default:
	```json
	"colorPalette": [
		"#000000",
		"#FFFFFF",
		"#00FFFF",
		"#0000FF",
		"#00FF00",
		"#FFA500",
		"#FFFF00",
		"#FF00FF",
		"#FF0000",
		"#800080"
	],
	```
	- [X] lock mode on
	- [X] activation hotkey default to `ctrl`+`alt`+`x`
	- [X] log level default to `Error`
	- [X] copyScreenshotToClipboard default to `true`
	- [X] playShutterSound default to `true`
	- [X] openFolderAfterScreenshot default to `true`
- [X] Splash screen uses `docs\hero-orig.png` (WPF splash + MSIX SplashScreen.png)
- [X] Tray about screen update with accurate info
- [X] Tool change toast bottom center screen fades away
- [X] Activation toast bottom center screen
  - [X] Show current tool
  - [X] How to exit. Depending on activation hotkey
  - [X] F1 for help
- [X] Ensure setting dir work in both msi version and win store version
- [X] All project build warnings, messages and errors are resolved
- [ ] All documents updated with correct information. Not needed documents removed.
- [X] Update versioning scripts to use 4 part version (Major.Minor.Build.Revision). The first release will use 2.0.0.0 and future revisions will bump the revision instead of patch. `Update-Version.ps1` needs to be updated.