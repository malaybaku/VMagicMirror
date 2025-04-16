---
layout: page
title: FAQ
lang: en
---

# FAQ
{: .no_toc }

<div class="toc-area" markdown="1">

#### Content
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### Control panel does not appear for the first app launch
{: .doc-sec2 }

Installation might be failed in this case. See [Download Troubleshoot](../download#troubleshoot_first_startup).

If the troubleshoot does not help, please contact via BOOTH message or Twitter DM.

Especially when you need Full Edition, you must use BOOTH message to trace the purchase status.


#### VMagicMirror stops soon after started
{: .doc-sec2 }

Setting file might be broken in this case. Try following steps to reset settings.

<div class="doc-ul" markdown="1">

1. Press `Reset` on the control panel `Home` tab, and then press `OK` on the confirmation dialog to reset settings.
    + If it recovers the situation, please follow [2: Get Started](../en_get_started.html) to setup.
2. If the problem still remains, exit VMagicMirror.
3. Delete auto save file. There are two ways according to the version of VMagicMirror.
    + (v2.0.7 or later) Open `(My Documents)\VMagicMirror_Files\Saves` folder and then delete `_autosave`, and `_preferences` file.
    + (v1.9.2 or later) Open `(My Documents)\VMagicMirror_Files\Saves` folder and then delete `_autosave` file.
4.  Restart VMagicMirror.

</div>

#### Too high CPU usage

In `Streaming` tab on control panel,

<div class="doc-ul" markdown="1">

1. (Large effect): Disable face tracking.
2. (Middle effect): Disable lipsync by microphone.
3. (Middle effect): Disable shadow, and wind.
4. (Small effect): Hide unused devices.

In setting window,

1. (Middle effect): `Effects` tab, set `Quality` to lower option.
2. (Small effect): `Layout` tab, disable gamepad input capture and MIDI input reading. 
3. (Small effect): `Effects` tab, set `Bloom`'s `Intensity` to 0.

</div>

If you still have high CPU usage it might be because of .vrm data structure. Please check it by using NOT heavy and officially opened model like Alicia Solid.

#### Eyes do not move by mouse pointer 
{: .doc-sec2 }

It is by specification, for some game software (please see detail in *note*).

Using fixed eye motion might improve appearance. 

In Control panel > `Streaming` tab > `Face` > `Eye Look Target`, select `None`.

*note*: Cause of the trouble is as following.

Some game runs program to move mouse position to the center of game window. 

(FPS games need this type of program to support mouse-based camera control without getting unexpected mouse position.)

It results the fixed mouse position and eye / head position of avatar, even if you are moving mouse physically.

One example of the popular software which leads the trouble is VRChat Desktop Mode.


#### Eye Blink tracking does not work
{: .doc-sec2 }

If you put on glasses, try without them.

Some frame with thick frame prevents face tracking system.

If not, please check following points to help face tracking system.

<div class="doc-ul" markdown="1">

1. Proper distance from camera
2. The room is bright
3. Neck and face outline is clear
4. Show mouth to the camera (*it is okay the microphone partly hide your mouth)

</div>

Showing entire face helps eye blink tracking, because face tracking system finds your face by detecting your whole face landmark points (including mouth, eyebrows, and of course eyes).

#### After loading VRM avatar window seems disappear
{: .doc-sec2 }

This issue might happen when you have changed display resolution or placement.

In this case, you can reset the window position.

<div class="doc-ul" markdown="1">

1. Move Control panel to near to the left top side of screen.
2. Open setting window and show `Window` tab to select `Reset Character Position`.
    + If you could find avatar then [2: Get Started](../get_started) will support your setup.
3. If the avatar still does not appear, then turn off `Transparent Background` on setting window `Window` tab and check if you can see green window at the right side of control panel.
4. Setting window `Layout` tab, see `Camera` menu, and press `Reset Position` to reset the camera position.
    + If you could find avatar then [2: Get Started](../get_started) will support your setup.

</div>

If you still have trouble, then please try the way in `VMagicMirror stops soon after started`.

#### Nothing happens after select .vrm on `Load VRM`
{: .doc-sec2 }

Security software can be the cause of this issue, as VMagicMirror operates interprocess communication between avatar window and control panel.

Please try to disable the security software in this case.

As far as the creator knows, COMODO Internet Security leads this issue, but it is just an example.

#### Shadow looks not good
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

* It is possible the avatar uses `Unlit` style shader and this case shadow might not appear.
* When your model is based on VRoidStudio and texture is partially transparent, some transparent part happens to be drawn with half transparent style.

</div>

If your trouble does not match above cases, then quality setting may be a help (however CPU usage increases). See the quality setting in `Effects` tab in setting window and select higher option.

#### VMagicMirror crashed after removing game controller
{: .doc-sec2 }

VMagicMirror might crash if you unplug the game controller during VMagicMirror is active.

Please restart VMagicMirror to recover. If VMagicMirror repeats to crash, then please reboot PC.

#### Face not rotate when using face tracking
{: .doc-sec2 }

This issue happens when VMagicMirror folder is put in the path which include multi-byte character.

Please quit VMagicMirror, move the folder to another (multi-byte character free) folder, and retry.


#### Transparent background does not work in Windows 11
{: .doc-sec2 }

When you have updated OS version and avatar window cannot get transparent background, please check next section `Select Transparent background, but get black background color`.

This issue might happen when you update NVIDIA graphics driver with `clean install` option, or install OS itself with clean install style.

For Windows 11 it also might be device dependent problem. Please contact to the developer if next section does not help.


#### Select Transparent background, but get black background color
{: .doc-sec2 }

This issue happens by NVIDIA control panel setting.

Quit VMagicMirror, and open NVIDIA control panel (right-click on desktop) to turn off:

`3D Setting` > `3D Setting Management` > `Anti-Aliasing FXAA`

Then, restart VMagicMirror.


#### Pen Tablet input is not tracked 
{: .doc-sec2 }

There are some possible causes, and some cases are by design.

VMagicMirror observes user's pen input as mouse pointer movement, so the tracking fails during another app prevents mouse pointer to move.

Following situations are the case.

<div class="doc-ul" markdown="1">

* Some of illustration software which support Wacom pen tablet
* Power Point with stylus pen input mode

</div>


#### How to uninstall VMagicMirror?
{: .doc-sec2 }

Uninstall process depends on version.

<div class="doc-ul" markdown="1">

- v1.9.0 or later: Open Windows setting's `Apps and Features`, and search VMagicMirror to remove it.
- v1.8.2 or older: Delete unzipped VMagicMirror folder.

</div>

If you are using v1.9.0 or later and you also want to remove settings files, then remove `(My Documents)\VMagicMirror_Files` folder.


#### Cannot connect to iFacialMocap
{: .doc-sec2 }

If your problem is by update process, then rebooting PC might fix it.

For other troubles, please see [Troubleshoot section of the page about iFacialMocap](../docs/external_tracker_ifacialmocap#troubleshoot).



#### Lipsync does not work
{: .doc-sec2 }

以下を順に確認します。

<div class="doc-ul" markdown="1">

- Reboot Windows.
- Disable [External Tracker](../docs/external_tracker), or enable it with `Apply LipSync using External Tracker Data` turned off.
- If anti-virus is installed other than Windows Defender, try to disable it for VMagicMirror. There are some user report that Kaspersky products might disturb getting microphone input.
- Check if other VRM model works, by using some stable models like VRoid Studio sample model.
- If you have multiple microphones, check other microphone works.
- If you have multiple microphones, remove them and try to connect for each single microphone.
- If above does not help, try reset settings according to `VMagicMirror stops soon after started` section, at the top of this page.

</div>

#### Is VRM 1.0 supported?
{: .doc-sec2 }

VRM 1.0 is supported since VMagicMirror v3.0.0.

Note that newer version also supports conventional VRM 0.x models.


#### I have request / question not referred in this page
{: .doc-sec2 }

Please use one of following way for other feedbacks.

<div class="doc-ul" markdown="1">

- [Twitter](https://twitter.com/baku_dreameater) DM
- [BOOTH Shop](https://baku-dreameater.booth.pm/) contact link
- [Make issue on GitHub](https://github.com/malaybaku/VMagicMirror/issues/new)

</div>

Note: 

<div class="doc-ul" markdown="1">

- GitHub issue can be seen by anyone.
- When you need to show some images or videos to share request or trouble detail, please avoid BOOTH shop message since it is not suited for sharing media files.

</div>
