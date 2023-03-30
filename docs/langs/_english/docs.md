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
| [Window](./window)          | Control character window                                      |
| [Face](./face)              | Adjust facial motion setting except [External Tracker App](./external_tracker).      |
| [Motion](./motion)          | Adjust character size related parameters and motion scale     |
| [Layout](./layout)          | Layout of camera and devices, and device based motion setting |
| [Effects](./effects)        | Light, shadow, bloom, and wind                                |
| [Devices](./devices)        | Connection settings for gamepad and MIDI controller           |
| [Expressions](./expressions)| Feature to move the character face and motion                 |
| [Hot Key](./hotkey)         | Customize shortcut key input to control settings              |
| [Setting Files](./setting_files) | Advanced features to manage setting files                |
|==================================|==========================================================|


#### Advanced Features 
{: .doc-sec2 }

Followings are advanced features of VMagicMirror available

|----------------------------------+-------------------------------------------------|
| Feature                          | What you can                                 |
|:--------------------------------:|:------------------------------------------------|
| [External Tracker App](./external_tracker)      | High quality tracking with iOS app (iFacialMocap) |
| [Accessory](./accessory)                    | Load image / 3D model and attach to the avatar  |
| [Hand Tracking](./hand_tracking)            | Web camera based hand tracking       |
| [Setting File Management](./setting_files)  | Setting file save/load features      |
| [Game Motion Input](./game_input)                  | Game(FPS)-like motion by your avatar |
|==================================|=================================================|

#### External Tracker
{: .doc-sec2 }

`External Tracker App`, `Accessory` and `Hand Tracking` features are available in the specific tab in control panel.

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

