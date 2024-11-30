---
layout: page
title: Change Device Textures
lang: en
---

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
* MIDI controller note area: `midi_note.png`
* MIDI controller knob area: `midi_knob.png`
* Pen, when pen tablet motion enabled: `pen.png`
* Pen tablet, when pen tablet motion enabled: `pen_tablet.png`
* Arcade stick parts: `arcade_stick.png`
* Car steering: `car_handle.png`
* (v3.9.0 and later) Manga effect key down: `manga_keydown.png`
* (v3.9.0 and later) Manga effect enter key down: `manga_enter_keydown.png`
* (v3.9.0 and later) Manga effect mouse click: `manga_click.png`
* (v3.9.0 and later) Manga effect gamepad button: `manga_gamepad_button.png`
* (v3.9.0 and later) Manga effect gamepad stick: `manga_gameoad_stick.png`

You do not have to put all of the images. Put files which you want to overwrite.

For the gamepad and pen, see following UV template. Car steering model only supports single-colored image based texture replace.

Other parts do not have UV template because those images are used as-is. First image in below shows the example to replace key and touch pad.

For the images that file name starts with `manga_`, aspect ratio should be 4:3.

After the setup, start `VMagicMirror.exe` to load the specified image.

<div class="note-area" markdown="1">

**NOTE**

Pen's UV template is for v2.0.5 and later version. This UV is not compatible with v2.0.4 and older version.

</div>

<div class="row">

{% include docimg.html file="/images/tips/change_texture.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/gamepad_template.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/pen_template.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

</div>

When you want to recover the setting as default, remove image files and restart VMagicMirror.
