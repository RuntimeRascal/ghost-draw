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
- [ ] Add feature to allow erasing. while in drawing mode, pressing and releasing `e` activated eraser mode.cursor needs to change to a custom eraser cursor.
- [ ] Add feature to allow drawing a line. while in drawing mode, pressing and releasing `l` activated drawLine mode.
- [ ] Add feature to allow drawing a square. while in drawing mode, pressing and releasing `s` activated drawSquare mode.
- [ ] Add feature to allow drawing a circle. while in drawing mode, pressing and releasing `c` activated drawCircle mode.

## Phase Three
- [ ] Create a installer.
	- [ ] Installer should create a optional start menu entry.
	- [ ] Installer should create a optional desktop shortcut.
	- [ ] Installer should optionally add the app to startup so it runs when user logs in.
	- [X] Installer should be able to uninstall the app cleanly removing all files and registry entries.
	- [X] Installer should be able to update the app to a new version if run again and a current version is already installed.
- [ ] Create github actions workflow to build and package the installer. And publish to realeases on github.




## Settings
Seams like my settings file settings sometimes get overwritten. I set the key combo to Ctrl + Shift + x but sometimes it gets reset to Ctrl + Shift + D.

Create a default settings json file that we use as defaults for non set settings and for re-creating the settings file if it is corrupted.

On startup load settings, if there is an error loading settings, show user via toast or message dialog that the settings file is corupted and will be re-recreated with defaults. Update the settings file content with default settings.
