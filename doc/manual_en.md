
Contact : [獏星(ばくすたー)@baku_dreameater](https://twitter.com/baku_dreameater)

# VMagicMirror Manual

Contents

* 1: Load Character
* 2: Window
* Tips 1: Fix the character position in desktop when started
* 3: Layout and character motion
* 4: Light
* 5: Startup
* Tips 2: How to make VMagicMirror a desktop mascot
* 6: Troubleshooting

## 1: Load Character

After start the application by double click `VMagicMirror.exe`, two windows will appear.

* Config Window (small)
* Character Window (large)

![Start Image](https://github.com/malaybaku/VMagicMirror/blob/master/doc/pic/started.png)

From config window, choose `Load VRM` and then select your `.vrm` file in your PC.

Then Character window shows meta data with license information. Confirm the license and click `OK` to proceed to load.

Then, the character will appear and he / she will move according to your mouse and keyboard input.


## 2: Window

Choose "Window" tab to adjust the chromakey, or you want the transparent background.

Background RGB changes the chromakey for some case you are streaming with chromakey-supported software.

Background transparency can be changed on `Transparent Window` check.

## Tips 1: Fix the character position in desktop when started

Following process can fix the character position when the application started.

* Check `Move window to specific position on startup`
* If the window is transparent, check `(When transparent) Drag the character to move`
* Drag the character to the position, and click `Check current window pos`
* Confirm `The setting is applied on next startup` checkbox is on

## 3: Layout

Choose "Layout" tab to adjust parameters about character's proportion, or motion.

Character's motion parameters should be changed when your character looks strange when typing the keyboard, like fingers float above, or go too below the keyboard.

`Waiting motion` is designed to look like breathing motion, but it might also look as if the character floats, when you set the large value for `Scale [%]`.

`Other Motion` supports to change where the character looks by mouse position, and lip sync. For the lip sync, please set the microphone input to enable function.

For the tall character, increase `Height` of the `Camera` to show the face, and increase `Height` and `Size` of the `Keyboard & Mouse Pad` so that typing motion looks more natural.

By default, the application has keyboard and mouse pad CG(mesh), but you can disable it on this tab.

## 4: Light

In "Light" tab you can set the light and bloom setting to improve the looking of the character.

NOTE: When your VRM has unlit shader the light has no effect for them.

## 5: Startup

In "Startup" tab you can switch the setting to load on next startup. After you are accustomed to VMagicMirror, please check each setting to save the time for the setup.

## Tips 2: How to make VMagicMirror a desktop mascot

Following steps enable the VMagicMirror to be a desktop mascot application!

### Step 1: Setup VMagicMirror

* Follow `1: Load Character` to show the character
* In `Window` tab, check `Transparent Window`
* Fix the position according to `Tips 1: How to fix the character position to appear`
* In `Home` tab, check `Load the same VRM on next startup`
* If you have customized layout or light, check those items on `Startup` tab

After the setup above, exit VMagicMirror once and restart, then you will see the character automatically appears.


### Step 2: Start VMagicMirror after Windows has started

You also can start VMagicMirror soon after Windows boot is completed.

* Right click `VmagicMirror.exe` and create a shortcut.
* Move the shortcut into the Windows Startup Folder.

Startup folder is like follows, with UserName for your actual user name (and perhaps you might need to change drive name from "C:\" to proper one).

`C:\Users\(UserName)\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup`

When you want to update VMagicMirror or disable the automatic start, then remove the shortcut in that folder.


## 6: Troubleshooting

* When the character does not move even you move the mouse, then please click the character window once.
* If the application crashes soon after start, then please check the directory `VMagicMirror.exe` exists and open `ConfigApp` directory, then delete following files.
    + `_currentBackground`
    + `_currentLayout`
    + `_currentVrm`
    + `_startup`

