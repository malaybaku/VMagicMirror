---
layout: page
title: Window
lang: en
---

# Window

`Window` tab supports BG color when the character window is not transparent, and also can toggle whether the window is always foreground or not.

<div class="row">
{% include docimg.html file="/images/docs/window_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### Basic Settings
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- `BG Image`: Set or clear background image. 
- `Transparent Window`: Check to make character window transparent. Available in Streaming tab. 
- `(When Transparent) Drag Character`: Check to enable drag-based move the character window when transparent. Available in Streaming tab. 
- `Always Foreground`: Check to place the character almost always foreground. Checked by default.
- `Reset Position`: Press this button to move character window just right to the control panel window. Use this function when you lost where the character window is.

#### Advanced Settings
{: .doc-sec2 }

- `Background`: Set background color by RGB.
- `Spout`: Enable or Disable Spout image output. Spout image can be used from several other apps like OBS Studio with Spout2 Plugin for OBS Studio.
    - `Resolution`: Select the resolution of Spout output. By using fixed resolution, you will get higher resolution image without making character window large.
- Transparency settings
    - `Transparent Level`: Select the avatar transparency condition from level 0 to 4. Default value is level 2. Level 0 means always NOT transparent, and level 4 makes character always transparent.
    - `Alpha when Transpanrent`: Set the transparency when the avatar is transparent. Higher value means opaque.
