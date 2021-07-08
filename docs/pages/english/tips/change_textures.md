---
layout: page
title: Change Device Textures
permalink: /en/tips/change_textures
lang_prefix: /en/
---

[Japanese](../../tips/change_textures)

# Tips: Change Device Textures

VMagicMirror can load custom texture for the keyboard's key, or touch pad. So far, only `png` image is available.

Before starting `VMagicMirror.exe`, open the following folder.

`(Folder where VMagicMirror.exe exists)/VMagicMirror_Data/StreamingAssets`

Put the images to replace in this folder. Please use specific file name for each purpose. Default appearance is applied when image file does not exist, and it is default behavior.

* Keyboard key image: `key.png`
* Touch pad: `pad.png`
* Gamepad body : `gamepad_body.png`
* Gamepad buttons: `gamepad_button.png`
* MIDI controller note area: `midi_note.png`
* MIDI controller knob area: `midi_knob.png`
* Pen, when pen tablet motion enabled: `pen.png`
* Pen tablet, when pen tablet motion enabled: `pen_tablet.png`
* Arcade stick parts: `arcade_stick.png`

<div class="note-area" markdown="1">

**NOTE**

MIDI controller related texture support is from v1.6.2.

</div>

Default image is applied if above images does not exist.

For the gamepad, please use single-color-only image to change gamepad color. 

After the setup, start `VMagicMirror.exe` to load the specified image.

{% include docimg.html file="/images/tips/change_texture.png" %}

When you want to recover the setting as default, remove image files and restart VMagicMirror.
