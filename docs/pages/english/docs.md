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


#### Setting Window
{: .doc-sec2 }

`VMagicMirror` has more features for custom. To access them, click `Open Setting Window` on `Home` tab in control panel.

{% include docimg.html file="/images/docs/setting_window.png" %}

Setting Window consists of 5 tabs.

|----------------------------------+---------------------------------------------------------------|
| Tab Name                         | Description                                                   |
|:--------------------------------:|:--------------------------------------------------------------|
| [Window](./docs/window)          | Control character window                                      |
| [Motion](./docs/motion)          | Adjust character size related parameters and motion scale     |
| [Layout](./docs/layout)          | Layout of camera and devices, and device based motion setting |
| [Effects](./docs/effects)        | Light, shadow, bloom, and wind                                |
| [Expressions](./docs/expressions)| Feature to move the character face and motion                 |
|==================================|===============================================================|

#### Setting Window: Reset Settings to default
{: .doc-sec2 }

In setting window, many of the setting category supports reset settings by click `Reset` button at the right of category name.

Below is an example to reset light settings to default.

<div class="row">
{% include docimg.html file="/images/docs/reset_setting_before.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/reset_setting_after.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

