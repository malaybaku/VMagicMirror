---
layout: page
title: Get Started
permalink: /en/get_started
lang_prefix: /en/
---

[Japanese](../get_started)


# Get Started

This page is about the basic usage of `VMagicMirror` after download.

This video also shows the setup process.

<iframe class="youtube" width="560" height="315" data-src="https://www.youtube.com/embed/kYk-YHqPeMU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

### 1. Start and Load Character
{: .doc-sec1 }

Start `VMagicMirror.exe` to show GUI window ("Control Panel"), and Green window to show character ("Character Window").

**NOTE:** If `VMagicMirror.exe` does not start correctly please check unzip settings. Right-click zip file to select `Property`, and check whether `Security` area exists at the bottom part. Check `Allow` and click `OK` to apply setting, then retry to unzip file.

{% include docimg.html file="./images/get_started/img00_005_before_unzip.png" customclass="col s12 m12 s12" imgclass="fit-doc-img" %}

When you close one of the window, the other window also closes and VMagicMirror quits. If you do not want to show control panel, then minimize it.

Click `Load VRM` button on `Home` tab in control panel to select your `.vrm` file on PC.

<div class="row">
{% include docimg.html file="./images/get_started/img00_015_started.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_020_load_vrm.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

After select the character you will see the license for the model. Confirm it and click `OK` to load character.

If you want to load the character automatically on next boot, then check `Load current VRM on next startup`.

<div class="row">
{% include docimg.html file="./images/get_started/img00_030_load_vrm_confirmation.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_040_after_loaded.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

**Hint:** If your character is not shown correctly, try `Adjust size by VRM` button. You can more chances to fix the layout later.

<div class="row">
{% include docimg.html file="./images/get_started/img00_160_not_good_layout_example.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_170_after_adjust.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>


### 2. Streaming Tab and Main Features
{: .doc-sec1 }

`Streaming` tab in control panel is to access all of main features of `VMagicMirror`.

{% include docimg.html file="./images/get_started/streaming_tab_overview.png" %}

#### 2.1. Window
{: .doc-sec2 }

Check on `Transparent Window` to make character window background to transparent. 

After setup transparent background, then confirm to check `(When Transparent) Drag the character`. You can left-click and drag the character during the check is on.

If you want to dragging feature, then turn off `Drag the character` after placing your avatar.

<div class="row">
{% include docimg.html file="./images/get_started/img00_060_transparent_bg.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_070_transparent_bg_drag.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_090_transparent_bg_can_click.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>


#### 2.2. Virtual Camera Output
{: .doc-sec2 }

`Virtual Camera Output` is a feature mainly for online meeting systems.

<div class="row">
{% include docimg.html file="./images/get_started/img00_095_virtual_cam_out.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

This feature is a bit advanced. If you are interested, see the detail at [Tips: Use Virtual Camera](./tips/virtual_camera).


#### 2.3. Face
{: .doc-sec2 }

`Face` can setup your VRM's face expressions.

<div class="row">
{% include docimg.html file="./images/get_started/img00_100_streaming_face.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

`LipSync`: Select microphone to use LipSync (viseme) feature.

`Track Face`: Select webcam to track your head motion

`Enable image based hand tracking`: Turn on to use camera image based minimal hand tracking.

If the model's face does not rotate in face tracking, please open [FAQ](./questions) and see "Face not rotate when using face tracking" section.

Hand tracking has some limitations, please see [Using Hand Tracking](./tips/using_hand_tracking).

If your face is not at the center for the web camera, or you are too near/far to the camera, the character indication might be unnatural. In this case, click `Calibrate position` to adjust.

**Hint:** When your character always look downward then look down and `Calibrate position` to adjust. It makes your avatar to look up always.


`Eye Look Target` sets where to see. In default you can use `Mouse` so that the character looks the orientation mouse pointer exists.


#### 2.4. Word To Motion
{: .doc-sec2 }

`Word To Motion` is feature to control face expression.

<div class="row">
{% include docimg.html file="./images/get_started/img00_105_word_to_motion.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

In default, please try typing "joy" and see what results on your avatar. Also you can select `Gamepad` on `Device Assignment` and press A,B,X,Y buttons, or select `Keyboard (num 0-8)` and press keys to switch expresssions.

Please see the detail in [Expressions](./docs/expressions).

When choosing `Device Assignment` to `Gamepad` or `MIDI Controller`, selected device arm motion is disabled. This helps you to hide what you do to change face expression from audience.


#### 2.5. Screenshot
{: .doc-sec2 }

Press camera icon `Take Picture` button to start 3 second count down, and the screenshot will be captured.

Screenshot is saved to the `Screenshots` folder in the folder where `VMagicMirror.exe` exists.

<div class="row">
{% include docimg.html file="./images/get_started/img00_180_screenshot.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_190_screenshot_shadow.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>


#### 2.6. View
{: .doc-sec2 }

Toggle the checks to set which device is visible and which is invisible.

Especially you can see the effect when keyboard is shown and selecting `Typing Effect` to something not None.

<div class="row">
{% include docimg.html file="./images/get_started/img00_125_view_typing_effect_example.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

**Hint:** If shadow looks bad, please check [FAQ](./questions) and "Shadow looks not good". If this does not help, then disable shadow.

#### 2.7. Camera
{: .doc-sec2 }

This "Camera" means eyesight on the character window.

When you use this feature turn off `Transparent Window` first. Then, check `Free Camera Mode` to move the point of view.

`Right Click + Drag`: Rotate eyesight.

`Middle Click + Drag`: Translate camera position.

`Middle wheel`: Move camera forward or backword.

After the setup it will be good to turn on `Transparent Background` and disable `Free camera mode`.

When you are confused where avatar is, press `Reset position` to recover the situation.

<div class="row">
{% include docimg.html file="./images/get_started/img00_130_free_camera_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_140_after_free_camera_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

During this setup you can use `Quick Save` and `Quick Load` buttons to save or load the point of view.

**Hint:** There is another way to setup camera position, with keeping `Transparent Window` enabled.

1. Check on `(When Transparent) Drag Character`
2. Left click the character
3. Adjust camera position
4. After adjusting, check off `(When Transparent)Drag Character`

In this process you might get trouble the character is cut off or going out of character window. In this case press `Reset Position`, or turn off `Transparent Window` to see the actual layout.

#### 2.8. Device Layout
{: .doc-sec2 }

Turn on `Free Layout` to enter device free layout mode.

{% include docimg.html file="./images/get_started/img00_200_free_layout.png" %}

When enter this mode `Transparent Window` is turned off.

During this mode the control UI appears at the top-left corner of character window.

`Control Mode`: Choose which parameter to change, position, rotation, or scale.

`Coordinate`: Choose the coordinate from device local, or world. If you are not clear about this, use `Local`.

`Gamepad Scale`: Adjust gamepad model size. If gamepad is too big for your avatar, then decrease this value.

`Reset`: Use this command to recover the standard layout.


#### 2.9. Motion
{: .doc-sec2 }

Check on `Presentation-like hand` to move VRM's right hand as if he / she is on a presentation.

<div class="row">
{% include docimg.html file="./images/get_started/img00_150_presentation_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

This style matches when you have presentation or you want to create instruction video.

See [Tips: Use VMagicMirror for Presentation](./tips/presentation) for the detail.


### 3. For further customize
{: .doc-sec1 }

This page is about basic use with control panel, and VMagicMirror also has more detailed menu in Setting Window.

See [Docs](./docs) or [Tips](./tips) for the detail.
