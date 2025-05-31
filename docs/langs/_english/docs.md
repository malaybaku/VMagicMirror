---
layout: page
title: Docs
lang: en
---

# Docs

This page introduces advanced settings in VMagicMirror.

For the basic usage, please see [Get Started](../get_started).

#### Setting Window
{: .doc-sec2 }

`VMagicMirror` has more features for custom. To access them, click `Open Setting Window` on `Home` tab in control panel.

{% include docimg.html file="/images/docs/setting_window.png" %}

Setting Window consists of 8 tabs.

|----------------------------------+----------------------------------------------------------|
| Tab Name                         | Description                                              |
|:--------------------------------:|:---------------------------------------------------------|
| [Window](./window)          | Control avatar window                                         |
| [Face](./face)              | Adjust facial motion settings of the avatar                   |
| [Motion](./motion)          | Adjust avatar size related parameters and motion scale        |
| [Layout](./layout)          | Layout of camera and devices, and device based motion setting |
| [Effects](./effects)        | Light, shadow, bloom, and wind                                |
| [Devices](./devices)        | Connection settings for gamepad and MIDI controller           |
| [Expressions](./expressions)| Feature to move the avatar face and motion                    |
| [VMC Protocol](./vmc_protocol) | Receive pose / facial data from VMC Protocol               |
| [Hot Key](./hotkey)         | Customize shortcut key input to control settings              |
| [Setting Files](./setting_files) | Advanced features to manage setting files                |
|==================================|==========================================================|


#### Advanced Features 
{: .doc-sec2 }

Followings are advanced features of VMagicMirror available

|----------------------------------+-------------------------------------------------|
| Feature                          | What you can                                    |
|:--------------------------------:|:------------------------------------------------|
| [Face Tracking](./face_tracker)  | Face Tracking with web camera or iOS app (iFacialMocap) |
| [VMC Protocol](./vmc_protocol)   | VMC Protocol based motion and facial            |
| [Accessory](./accessory)         | Load image / 3D model and attach to the avatar  |
| [Buddy](./buddy)                 | Load buddy character                            |
| [Hand Tracking](./hand_tracking)            | Web camera based hand tracking       |
| [Game Motion Input](./game_input)                  | Game(FPS)-like motion by your avatar |
| [Setting File Management](./setting_files)  | Setting file save/load features      |
|==================================|=================================================|

#### External Tracker
{: .doc-sec2 }

`Face Tracking`, `Accessory`, `Buddy` and `Hand Tracking` features are available in the specific tab in control panel.

`Setting File Management` is mainly about feature available in `Home` tab.

`Game Input` is new feature from VMagicMirro v3.1.0. Turn on the feature by opening `Streaming` tab > `Motion` > `Body Motion Style` and select `Game Input`.

#### Setting Window: Reset Settings to default
{: .doc-sec2 }

In setting window, many of the setting category supports reset settings by click `Reset` button at the right of category name.

Below is an example to reset light settings to default.

<div class="row">
{% include docimg.html file="/images/docs/reset_setting_before.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/reset_setting_after.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

