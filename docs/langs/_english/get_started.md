---
layout: page
title: Get Started
lang: en
---


# Get Started
{: .no_toc }

This page is about the basic usage of `VMagicMirror` after download.

This video also shows the setup process.

Note that, this video refers older version, so some GUI has changed.

<iframe class="youtube" width="560" height="315" data-src="https://www.youtube.com/embed/kYk-YHqPeMU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<div class="toc-area" markdown="1">

#### Content
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

### 1. Start and Load Avatar
{: .doc-sec1 }

First step depends on the version of `VMagicMirror` you have.

Please see one of following sections `1-1. Start v1.9.0 or later version` or `1-2. Start v1.8.2 or older version`.

#### 1-1. Start v1.9.0 or later version
{: .doc-sec2 }

Newer version of `VMagicMirror` is distributed with the form of installer (.exe) file.

Unzip the distributed file and run installer. After the installation, launch `VMagicMirror` from shortcut, or search in start menu to launch.

<div class="note-area" markdown="1">

**NOTE**

Installer run might be blocked by Windows system. In this case, right click installer file (or zip file) and open `Property` to check whether `Security` area exists at the bottom. Check `Allow` and click `OK` to apply setting, then retry to run installer.

<div class="row">
{% include docimg.html file="./images/get_started/img00_004_remove_block_of_installer.jpg" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

</div>


#### 1-2. Start v1.8.2 or older version
{: .doc-sec2 }

Unzip the distributed file and start `VMagicMirror.exe` in the folder.

<div class="note-area" markdown="1">

**NOTE**

If `VMagicMirror.exe` does not start correctly please check unzip settings. Right-click zip file to select `Property`, and check whether `Security` area exists at the bottom part. Check `Allow` and click `OK` to apply setting, then retry to unzip file.

<div class="row">
{% include docimg.html file="./images/get_started/img00_005_before_unzip.jpg" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Also please confirm the unzip folder is normal folder (like `C:\` or `My Document` folder).

Please avoid special folder like `Program Files`, as those folder will lead tons of problem.

</div>


#### 1-3. Load Model after Start
{: .doc-sec2 }

After the app started there appears 2 windows. One window shows GUI ("Control Panel"), and the other window shows avatar ("Avatar Window").

When you close one of the window, the other window also closes and VMagicMirror quits. If you do not want to show control panel, then minimize it.

<div class="row">
{% include docimg.html file="./images/get_started/img00_015_started.jpg" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

If control panel window does not appear, it indicates install might not be successful.

See [Download Troubleshoot](../download#troubleshoot_first_startup) in this case.

</div>

There are 2 ways to load avatar, from VRM file on the PC or from VRoid Hub.

To use the file on PC, click `Load from File` to select file. Confirm the license and click `OK` to load the avatar.

<div class="row">
{% include docimg.html file="./images/get_started/img00_020_load_vrm.jpg" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_030_load_vrm_confirmation.jpg" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}
</div>

To use the model in VRoid Hub click `Load from VRoid Hub`, then avatar window will shows VRoid Hub loader UI, with authentication instruction in the first time.

After login, choose the avatar and check condition of use, then load.

<div class="row">
{% include docimg.html file="./images/get_started/img00_032_connect_vroid_hub.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_034_vroid_hub_characters.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_037_vroid_hub_confirmation.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

Some avatars by other authors are not available even if you liked it on VRoid Hub. 

This is because of the usage limitation setting by the authors.

See the detail at [Tips: Use VRoid Hub Avatar](../tips/use_vroid_hub).

</div>

If you want to load the avatar automatically on next boot, then check `Load current VRM on next startup`.

<div class="row">
{% include docimg.html file="./images/get_started/img00_040_after_loaded.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

Also, if your avatar is not shown correctly, try `Adjust size by VRM` button. You can more chances to fix the layout later.

<div class="row">
{% include docimg.html file="./images/get_started/img00_160_not_good_layout_example.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_170_after_adjust.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>


### 2. Streaming Tab and Main Features
{: .doc-sec1 }

`Streaming` tab in control panel is to access all of main features of `VMagicMirror`.

{% include docimg.html file="./images/get_started/img00_050_streaming_tab.jpg" %}

#### 2.1. Window
{: .doc-sec2 }

Turn on `Transparent Window` to make avatar window to transparent. 

When the window is transparent and `(When Transparent) Drag the avatar` is turned on, you can left-click and drag to move the avatar.

If you do not want the dragging feature, then turn off `Drag the avatar` after placing your avatar.

<div class="row">
{% include docimg.html file="./images/get_started/img00_060_transparent_bg.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_070_transparent_bg_drag.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_090_transparent_bg_can_click.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

Also you can set background image when `Transparent Window` is turn off, from `Load` button on `BG Image`.


#### 2.2. Face
{: .doc-sec2 }

`Face` can setup your VRM's face expressions.

<div class="row">
{% include docimg.html file="./images/get_started/img00_100_streaming_face.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

<div class="doc-ul" markdown="1">

- `LipSync`: Select microphone to use LipSync (viseme) feature.
- `Sensitivity [dB]`: Specify plus value when the microphone input is too small, to obtain good result for lipsync.
- `Show Volume`: Turn on to see the input volume. Adjust `Sensitivity` such that, the volume bar is green and sometimes red during talking.
- `Track Face`: Select webcam to track your head motion.

</div>

If the model's face does not rotate in face tracking, please open [FAQ](../questions) and see "Face not rotate when using face tracking" section.

In `Face Tracking` tab more detailed settings are available. See detail at [Face Tracking](../docs/face_tracker).


#### 2.3. Motion
{: .doc-sec2 }

Motion menu support to custom how the avatar moves by user inputs.

You can select the reaction when using keyboard/mouse, and gamepad separately.

Also this menu has `Hands-Down Mode` check, which forces avatar's arms always down. This mode also increases the body movement. In this mode the body movement increases slightly.

<div class="row">
{% include docimg.html file="./images/get_started/img00_210_motion_modes.jpg" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_220_motion_modes_hand_down.jpg" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

#### 2.4. View
{: .doc-sec2 }

Toggle the checks to set which device is visible and which is invisible.

Especially you can see the effect when keyboard is shown and selecting `Typing Effect` to something not None.

<div class="row">
{% include docimg.html file="./images/get_started/img00_125_view_typing_effect_example.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**Hint**

If shadow looks bad, please check [FAQ](../questions) and "Shadow looks not good". 

If this does not help, then disable shadow.

</div>

#### 2.5. Camera
{: .doc-sec2 }

This "Camera" means eyesight on the avatar window.

When you use this feature turn off `Transparent Window` first. Then, check `Free Camera Mode` to move the point of view.

<div class="doc-ul" markdown="1">

- `Middle wheel`: Move camera forward or backword.
- `Right Click + Drag`: Rotate eyesight.
    - In v2.0.1 or later `Alt Key + Left Click + Drag` also rotates eyesight.
- `Middle Click + Drag`: Translate camera position.
    - In v2.0.1 or later `Shift Key + Left Click + Drag` also translates camera.

</div>

When you are confused where avatar is, press `Reset position` to recover the situation.

<div class="row">
{% include docimg.html file="./images/get_started/img00_130_free_camera_mode.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

During this setup you can use `Quick Save` and `Quick Load` buttons to save or load the point of view.

<div class="note-area" markdown="1">

**Hint** 

There is another way to setup camera position, keeping `Transparent Window` on.

1. Check on `(When Transparent) Drag Avatar`
2. Left click the avatar
3. Adjust camera position
4. After adjusting, check off `(When Transparent)Drag Avatar`

This is useful, but you will be more easily lost the avatar out of the window area.

In this case press `Reset Position`, or turn off `Transparent Window` to see the actual layout.

</div>

#### 2.6. Device Layout
{: .doc-sec2 }

Turn on `Free Layout` to enter device free layout mode.

{% include docimg.html file="./images/get_started/img00_200_free_layout.jpg" %}

When enter this mode `Transparent Window` is turned off.

During this mode the control UI appears at the top-left corner of avatar window.

<div class="doc-ul" markdown="1">

- `Control Mode`: Choose which parameter to change, position, rotation, or scale.
- `Coordinate`: Choose the coordinate from device local, or world. If you are not clear about this, use `Local`.
- `Gamepad Scale`: Adjust gamepad model size. If gamepad is too big for your avatar, then decrease this value.

</div>

Use `Reset` to recover the standard layout.


#### 2.7. Word To Motion
{: .doc-sec2 }

`Word To Motion` is feature to control face expression.

<div class="row">
{% include docimg.html file="./images/get_started/img00_105_word_to_motion.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

In default, please try typing "joy" and see what results on your avatar. Also you can select `Gamepad` on `Device Assignment` and press A,B,X,Y buttons, or select `Keyboard (num 0-8)` and press keys to switch expresssions.

Please see the detail in [Expressions](../docs/expressions).

When choosing `Device Assignment` to `Gamepad` or `MIDI Controller`, selected device arm motion is disabled. This helps you to hide what you do to change face expression from audience.


### 3. For further customize
{: .doc-sec1 }

The pages below documents more about VMagicMirror's specification.

<div class="doc-ul" markdown="1">

- [Docs](../docs): Documents about Detail Setting Window and iOS collaboration (Ex.Tracker in main window).
- [Tips](../tips): Show use-case based tips and some special settings which does not have GUI.

</div>
