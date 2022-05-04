---
layout: page
title: Change Device Textures
permalink: /en/tips/change_textures
lang: en
---

[Japanese](../../tips/change_textures)

# Tips: Change Device Textures

VMagicMirror can load custom texture for the keyboard's key, or touch pad. So far, only `png` image is available.

Before starting `VMagicMirror.exe`, open the following folder. 

<div class="doc-ul" markdown="1">

- v1.9.0 or later: `(My Documents)\VMagicMirror_Files\Textures`
- v1.8.2 or older: `(VMagicMirror.exe folder)\VMagicMirror_Data\StreamingAssets`

</div>

Put the images to replace in this folder. Please use specific file name for each purpose. Default appearance is applied when image file does not exist, and it is default behavior.

* Keyboard key image: `key.png`
* Touch pad: `pad.png`
* Gamepad body : `gamepad_body.png`
* Gamepad buttons: `gamepad_button.png` (*v1.8.1 or older)
* MIDI controller note area: `midi_note.png`
* MIDI controller knob area: `midi_knob.png`
* Pen, when pen tablet motion enabled: `pen.png`
* Pen tablet, when pen tablet motion enabled: `pen_tablet.png`
* Arcade stick parts: `arcade_stick.png`

<div class="note-area" markdown="1">

**NOTE**

MIDI controller related texture support is from v1.6.2.

</div>

You do not have to put all of the images, so just put files which you want to overwrite.

Gamepad texture has different requirement depend on VMagicMirror version. 

For v1.8.2 or later, single `gamepad_body.png` file supports body and button texture, and UV template is as following.

v1.8.1 and older version requires single-color-only image for body and button separatedly, so you will need to put `gamepad_body.png` and `gamepad_button.png`.

After the setup, start `VMagicMirror.exe` to load the specified image.

<div class="row">

{% include docimg.html file="/images/tips/change_texture.png" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/gamepad_template.png" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}

</div>

When you want to recover the setting as default, remove image files and restart VMagicMirror.
