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

|----------------------------------+---------------------------------------------------------------|
| Tab Name                         | Description                                                   |
|:--------------------------------:|:--------------------------------------------------------------|
| [Window](./window)          | Control character window                                      |
| [Face](./face)              | Adjust facial motion setting except [External Tracker App](./external_tracker).      |
| [Motion](./motion)          | Adjust character size related parameters and motion scale     |
| [Layout](./layout)          | Layout of camera and devices, and device based motion setting |
| [Effects](./effects)        | Light, shadow, bloom, and wind                                |
| [Devices](./devices)        | Connection settings for gamepad and MIDI controller          |
| [Expressions](./expressions)| Feature to move the character face and motion                 |
| [Setting Files](./setting_files) | Advanced features to manage setting files |
|==================================|===============================================================|


#### External Tracker
{: .doc-sec2 }

This feature supports external application to move avatar with high precision.

Current version (v2.0.0) supports iOS app `iFacialMocap`.

Please see the detail at [External Tracker App](./external_tracker) page.

#### Accessory
{: .doc-sec2 }

v2.0.0 or later version supports accessory feature, which can load png image and some 3D models into the application.

Please see detail in [Accessory](./accessory) page.


#### Hand Tracking
{: .doc-sec2 }

v1.8.0 and later version supports `Hand Tracking` tab in control panel, to support webcam based hand tracking.

Please see more in [Hand Tracking](./hand_tracking) page.


#### Setting File Management

VMagicMirror v1.6.2 and later version supports setting file save and load internally.

Detail is in [Setting Files](./setting_files) page.

This page refers both control panel `Home` tab functions and setting window `File` tab.


#### Setting Window: Reset Settings to default
{: .doc-sec2 }

In setting window, many of the setting category supports reset settings by click `Reset` button at the right of category name.

Below is an example to reset light settings to default.

<div class="row">
{% include docimg.html file="/images/docs/reset_setting_before.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/reset_setting_after.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

