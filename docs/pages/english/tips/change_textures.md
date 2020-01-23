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

Put the images to replace in this folder. File name must be following.

* For the keyboard key image: `key.png`
* For the touch pad: `pad.png`
* For the gamepad body : `gamepad_body.png`
* For the gamepad buttons: `gamepad_button.png`

Default image is applied if above images does not exist.

For the gamepad, please use single-color-only image to change gamepad color. 

After the setup, start `VMagicMirror.exe` to load the specified image.

{% include docimg.html file="/images/tips/img_tips_10_change_texture.png" %}

When you want to recover the setting as default, remove image files and restart VMagicMirror.
