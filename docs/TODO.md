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

## Phase Three
- [ ] Create a installer.
	- [ ] Installer should create a optional start menu entry.
	- [ ] Installer should create a optional desktop shortcut.
	- [ ] Installer should optionally add the app to startup so it runs when user logs in.
	- [X] Installer should be able to uninstall the app cleanly removing all files and registry entries.
	- [X] Installer should be able to update the app to a new version if run again and a current version is already installed.
- [X] Create github actions workflow to build and package the installer. And publish to releases on github.

**New plan**
- Create MSIX and publish to MS Store. This will prevent needing a expensive software sign cert in order to self sign the product and prevent looking like a sus product from win security messaging during download and install. Publishing to ms store is a 1-time $10 charge.

