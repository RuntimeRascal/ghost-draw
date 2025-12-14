# TODO List

## Phase One
- [X] Settings window settings persist. The window needs to look modern and have innovative design that matches the name of the app and what it does. We can use the logo in Assets and the window should not have the default windows title bar instead look much more modern. Use a fancy gradient as the window background.
- [X] Custom ghost/pencil icon in the system tray.
- [X] Allow changing the color of the drawing. Can change in settings that persist. Allow cycling through color options by right clicking on the mouse.
- [X] Allow changing the size/brush thickness of the drawing. Can change in settings that persist. Allow increasing/decreasing the size with the scroll wheel. 
- [X] Have a max and a min size setting.
- [X] Create custom colored pencil cursor. The cursor will have the tip colored to match the currently drawing color.
- [X] Settings allows choosing keyboard combo to activate drawing mode.
- [X] Setting to lock drawing mode. If drawing mode is locked setting is true when press key combo we lock the drawing mode in and no longer require user to hold the combo. Drawing mode continues until the user presses the key combo again or ESC is pressed.

## Phase Two
- [X] Get the settings window glow effect working.
- [X] Add feature to allow erasing. while in drawing mode, pressing and releasing `e` activated eraser mode.cursor needs to change to a custom eraser cursor.
- [X] Add feature to allow drawing a line. while in drawing mode, pressing and releasing `l` activated drawLine mode.
- [X] Add feature to allow drawing a square. while in drawing mode, pressing and releasing `s` activated drawSquare mode.
- [X] Add feature to allow drawing a circle. while in drawing mode, pressing and releasing `c` activated drawCircle mode.
- [X] Add feature to allow typing text.
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
- [ ] Tool change toast bottom center screen fades away
- [ ] Activation toast bottom center screen
  - [ ] Show current tool
  - [ ] How to exit. Depending on activation hotkey
  - [ ] F1 for help
- [X] Ensure setting dir work in both msi version and win store version
- [X] All project build warnings, messages and errors are resolved
- [ ] All documents updated with correct information. Not needed documents removed.
- [X] Update versioning scripts to use 4 part version (Major.Minor.Build.Revision). The first release will use 2.0.0.0 and future revisions will bump the revision instead of patch. `Update-Version.ps1` needs to be updated.