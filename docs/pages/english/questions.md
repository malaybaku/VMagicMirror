---
layout: page
title: FAQ
permalink: /en/questions
lang_prefix: /en/
---

[Japanese](../questions)

# FAQ

#### VMagicMirror stops soon after started
{: .doc-sec2 }

Setting file might be broken in this case, so please try to reset the settings.

1. Press `Reset` on the control panel `Home` tab, and then press `OK` on the confirmation dialog to reset settings.
    + If it recovers the situation, please follow [2: Get Started](./en_get_started.html) to setup.
2. If the problem still remains, exit VMagicMirror.
3. Open the folder in which `VMagicMirror.exe` exists, and then delete `_autosave` file in `ConfigApp` folder. Then try to restatt.

#### Too high CPU usage

In `Streaming` tab on control panel,

1. (Large effect): Disable face tracking.
2. (Middle effect): Disable lipsync by microphone.
3. (Middle effect): Disable shadow, and wind.
4. (Small effect): Hide unused devices.

In setting window,

1. (Middle effect): `Effects` tab, set `Quality` to lower option.
2. (Small effect): `Layout` tab, disable gamepad input capture and MIDI input reading. 
3. (Small effect): `Effects` tab, set `Bloom`'s `Intensity` to 0.

If you still have high CPU usage it might be because of .vrm data structure. Please check it by using NOT heavy and officially opened model like Alicia Solid.

#### Eyes do not move by mouse pointer 
{: .doc-sec2 }

It is by specification, for some game software (please see detail in *note*).

Using fixed eye motion might improve appearance. 

* Control panel > `Streaming` tab > `Face` > `Eye Look Target` > select `None`


*note*: Cause of the trouble is as following.

Some game runs program to move mouse position to the center of game window. 

(FPS games need this type of program to support mouse-based camera control without getting unexpected mouse position.)

It results the fixed mouse position and eye / head position of character, even if you are moving mouse physically.

One example of the popular software which leads the trouble is VRChat Desktop Mode.


#### Eye Blink tracking does not work
{: .doc-sec2 }

If you put on glasses, try without them.

Some frame with thick frame prevents face tracking system.

If not, please check following points to help face tracking system.

1. Proper distance from camera
2. The room is bright
3. Neck and face outline is clear
4. Show mouth to the camera (*it is okay the microphone partly hide your mouth)

Showing entire face helps eye blink tracking, because face tracking system finds your face by detecting your whole face landmark points (including mouth, eyebrows, and of course eyes).

#### After loading VRM character window seems disappear
{: .doc-sec2 }

This issue might happen when you have changed display resolution or placement.

In this case, you can reset the window position.

1. Move Control panel to near to the left top side of screen.
2. Open setting window and show `Window` tab to select `Reset Character Position`.
    + If you could find character then [2: Get Started](./get_started) will support your setup.
3. If the character still does not appear, then turn off `Transparent Background` on setting window `Window` tab and check if you can see green window at the right side of control panel.
4. Setting window `Layout` tab, see `Camera` menu, and press `Reset Position` to reset the camera position.
    + If you could find character then [2: Get Started](./get_started) will support your setup.

If you still have trouble, then please try the way in `VMagicMirror stops soon after started`.

#### Nothing happens after select .vrm on `Load VRM`
{: .doc-sec2 }

Security software can be the cause of this issue, as VMagicMirror operates interprocess communication between character window and control panel.

Please try to disable the security software in this case.

As far as the creator knows, COMODO Internet Security leads this issue, but it is just an example.

#### Shadow looks not good
{: .doc-sec2 }

* It is possible the character uses `Unlit` style shader and this case shadow might not appear.
* When your model is based on VRoidStudio and texture is partially transparent, some transparent part happens to be drawn with half transparent style.

If your trouble does not match above cases, then quality setting may be a help (however CPU usage increases). See the quality setting in `Effects` tab in setting window and select higher option.

#### Want to hide the circle mark around the mouse pointer during `Presentation-like hand` is on
{: .doc-sec2 }

In the setting window `Motion` tab, see `Arm` and turn off `Show Pointer Support` to hide the mark.

#### VMagicMirror crashed after removing game controller
{: .doc-sec2 }

VMagicMirror might crash if you unplug the game controller during VMagicMirror is active.

Please restart VMagicMirror to recover. If VMagicMirror repeats to crash, then please reboot PC.
