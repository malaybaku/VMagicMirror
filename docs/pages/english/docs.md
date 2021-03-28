---
layout: page
title: Docs
permalink: /en/docs
lang_prefix: /en/
---

[Japanese](../docs)

# Docs

This page introduces two advanced settings in VMagicMirror.

For the basic usage, please see [Get Started](./get_started).


#### External Tracker
{: .doc-sec2 }

VMagicMirror v1.1.0 and later version supports external application to move avatar.

Currently v1.1.0 supports iOS app `iFacialMocap`.

Please see the detail at [External Tracker App](./docs/external_tracker).

#### Setting File Management

VMagicMirror v1.6.2 and later version supports setting file save and load internally.

Detail is in [Setting Files](./docs/setting_files) page.

This page refers both control panel `Home` tab functions and setting window `File` tab.

#### Setting Window
{: .doc-sec2 }

`VMagicMirror` has more features for custom. To access them, click `Open Setting Window` on `Home` tab in control panel.

{% include docimg.html file="/images/docs/setting_window.png" %}

Setting Window consists of 8 tabs.

|----------------------------------+---------------------------------------------------------------|
| Tab Name                         | Description                                                   |
|:--------------------------------:|:--------------------------------------------------------------|
| [Window](./docs/window)          | Control character window                                      |
| [Face](./docs/face)              | Adjust facial motion setting except [External Tracker App](./docs/external_tracker).      |
| [Motion](./docs/motion)          | Adjust character size related parameters and motion scale     |
| [Layout](./docs/layout)          | Layout of camera and devices, and device based motion setting |
| [Effects](./docs/effects)        | Light, shadow, bloom, and wind                                |
| [Devices](./docs/devices)        | Connection settings for gamepad and MIDI controller          |
| [Expressions](./docs/expressions)| Feature to move the character face and motion                 |
| [Setting Files](./docs/setting_files) | Advanced features to manage setting files |
|==================================|===============================================================|

<div class="note-area" markdown="1">

**NOTE**

v1.5.0 and older versions do not have `Devices` tab, and instead have same feature in `Layout` tab.

</div>

#### Setting Window: Reset Settings to default
{: .doc-sec2 }

In setting window, many of the setting category supports reset settings by click `Reset` button at the right of category name.

Below is an example to reset light settings to default.

<div class="row">
{% include docimg.html file="/images/docs/reset_setting_before.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/reset_setting_after.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

