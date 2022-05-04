---
layout: page
title: Window
permalink: /en/docs/window
lang: en
---

[Japanese](../../docs/window)

# Window

`Window` tab supports BG color when the character window is not transparent, and also can toggle whether the window is always foreground or not.

<div class="row">
{% include docimg.html file="/images/docs/window_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### Features
{: .doc-sec2 }

`Background`: Set background color by RGB.

`BG Image`: Set or clear background image. 

`Transparent Window`: Check to make character window transparent. Available in Streaming tab. 

`(When Transparent) Drag Character`: Check to enable drag-based move the character window when transparent. Available in Streaming tab. 

`Always Foreground`: Check to place the character almost always foreground. Checked by default.

`Reset Position`: Press this button to move character window just right to the control panel window. Use this function when you lost where the character window is.

`Virtual Camera Output`: Support same menu as `Streaming` tab. Please see the detail at [Tips: Use Virtual Camera](../tips/virtual_camera).

`Transparent Level`: Select the character transparency condition from level 0 to 4. Default value is level 2. Level 0 means always NOT transparent, and level 4 makes character always transparent.

`Alpha when Transpanrent`: Set the transparency when the character is transparent. Higher value means opaque.

#### NOTE: Schedule of virtual camera feature removal (v1.6.0)

`Window` tab also supports [Virtual Camera](../tips/virtual_camera) feature, but this function will be removed in v1.6.0.

This is because OBS Studio](https://obsproject.com/download) has introduced virtual camera by standard, from version 26.0.

If you want to use VMagicMirror with virtual camera, please consider installing `OBS Studio`.
