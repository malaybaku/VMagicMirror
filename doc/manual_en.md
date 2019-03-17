
Contact : [獏星(ばくすたー)@baku_dreameater](https://twitter.com/baku_dreameater)

# VMagicMirror Manual

Contents

* 1: Load Character
* 2: Window
* Tips 1: How to fix the character position to appear
* 3: Layout
* 4: Light
* 5: Startup
* Tips 2: How to make VMagicMirror a desktop mascot
* 6: Troubleshooting

## 1: Load Character

After start the application by double click `VMagicMirror.exe`, two windows will appear.

* Config Window (small)
* Character Window (large)

![Start Image](https://github.com/malaybaku/VMagicMirror/blob/master/doc/pic/started.png)

From config window, choose "VRMロード" button and then select `.vrm` file in your PC.

Then Character window shows meta data including license notation. Confirm the license and click `OK` to proceed actual loading.

Then, the character will appear and he / she will move according to your mouse and keyboard input.


## 2: Window

Choose "Window" tab to adjust the chromakey, or you want the transparent background.

`R`, `G`, `B` means the background color in RGB.

`Intensity [%]` means the light intensity in percentage.

Background transparency can be changed on `Transparent Window` check.

## Tips 1: How to fix the character position to appear

Following process can fix the character position when the application started.

* Check `Move the window to specific position`
* If the window is transparent, check `(When transparent) Drag the character to move`
* Drag the character to the position to fix, then click `Get the current position`
* Confirm `The setting is applied on next startup` checkbox is on

## 3: Layout

Choose "Layout" tab to adjust parameters about character's proportion, or motion.

Character's motion parameters should be changed when your character looks strange when typing the keyboard.

Waiting motion assumes the motion as if the character breathes, but it also looks like floating when you set the large value for `Motion  Scale`.

In default the application has keyboard and mouse pad CG(mesh), but you can disable it on this tab.

## 4: Light

In "Light" tab you can set the light and bloom setting to improve the looking of the character.

## 5: Startup

In "Startup" tab you can switch the setting to load on next startup. After you are accustomed to use VMagicMirror, please check each setting to save the time for setup your character.

## Tips 2: How to make VMagicMirror a desktop mascot

Following steps enable the VMagicMirror to be a desktop mascot application!

### Step 1: Setup VMagicMirror

* Follow `1: Load Character` to show the character
* In "Window" tab, check "Transparent Window"
* Fix the position according to `Tips 1: How to fix the character position to appear`
* In "Home" tab, check "Load the same VRM on next startup"
* If you have customized layout or light, check the related item on "Startup" tab

After the setup above, exit VMagicMirror once and restart, then you will see the things is great!

### Step 2: Start VMagicMirror after Windows has started

You also can start VMagicMirror automatically after the Windows process has started.

* Right click on `VmagicMirror.exe` and create a shortcut
* Move the shortcut into the Windows Startup Folder.

Startup folder is like follows, with UserName for your actual user name.

C:\Users\(UserName)\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup

When you are updating VMagicMirror or you want to disable the automatic start, then remove the shortcut.


## 6: Troubleshooting

* When the character does not move even you move the mouse, then please click the character window once.
* If the application crashes soon after start, then please check the directory `VMagicMirror.exe` exists and open `ConfigApp` directory, then delete following files.
    + `_currentBackground`
    + `_currentLayout`
    + `_currentVrm`
    + `_startup`

