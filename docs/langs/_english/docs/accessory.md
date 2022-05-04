---
layout: page
title: Accessory
permalink: /en/docs/accessory
lang: en
---

[Japanese](../../docs/accessory)

# Accessory

This page is about accessory feature, in VMagicMirror v2.0.0 or later version.

<div class="row">
{% include docimg.html file="./images/docs/accessory_header.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

*See bottom section about license of the 3D model used in this page's screenshots.

#### What is Accessory Feature?
{: .doc-sec2 }

Accessory feature supports loading images and 3D models to attach to avatar's body.

<div class="doc-ul" markdown="1">

- Supported image format is png.
- Supported 3D model format is glb and glTF.
- v2.0.1 and later versions support numbered png for animated accessory.

</div>

Glb and glTF are the file format which is the base of avatar's format (VRM).

Please see [Get GLB Data](../tips/get_glb_data) tips about how to get those 3D model files.


#### Usage
{: .doc-sec2 }

First, put supported format files or folder into `(My Document)\VMagicMirror_Files\Accessory` folder.

In the case of glTF, you need to put by folder, and need to edit folder name to be meaningful.

<div class="note-area" markdown="1">

**NOTE**

If you edit the file/folder name after the accessory was loaded by VMagicMirror, then the item placement will be reset.

</div>

Use `Reload` button to reload files, if you have placed them after VMagicMirror started.

<div class="row">
{% include docimg.html file="./images/docs/accessory_folder_structure.png" customclass="col s6 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/docs/accessory_item_edit.png" customclass="col s6 m4 l4" imgclass="fit-doc-img" %}
</div>

After the accessory loading process, you can see the list of accessories, and checkbox at the top of them can swtich the visibility.

You can expand and edit each accessories' properties.

<div class="doc-ul" markdown="1">

- `Display Name`: The name, which is displayed when the UI is folded.
- `Attach to `: Select where to attach the accessory.
- `2D Foreground Mode`: Show the accessory always foreground. (Detail is in later)
- `Position`: Specify the local position. Also editable by `Free Layout`.
- `Rotation`: Specify the local rotation. Also editable by `Free Layout`.
- `Scale`: Specify the scale. Also editable by `Free Layout`.
- `FPS`: Available on numbered png, to decide how fast to play animation.

</div>

`2D Foreground Mode` is available for image file based accessory.

During this mode is enabled, the position of the accessory is calculated by the position if the mode is disabled.

<div class="note-area" markdown="1">

**NOTE**

It is a bit difficult to adjust accessory's position for 2D Foreground Mode. Following is one of the recommended way.

<div class="doc-ul" markdown="1">

- Turn off `2D Foreground Mode`, and adjust accessory's pose by `Free Layout`, to attach them at specific position (e.g. eyes or mouth).
- Then, turn on `2D Foreground Mode` to check the appearance is what you want.
- If further adjust needed, then edit in control panel's text, or again turn off `2D Foreground Mode` to adjust it by `Free Layout`.

</div>

</div>


#### About License of 3D Model in Screenshots
{: .doc-sec2 }

[Low poly Christmas deer horns accessory](https://sketchfab.com/3d-models/low-poly-christmas-deer-horns-accessory-5e5d4500345445cfa5dc7848ebd278ba) by 3D Bear is licensed under [Creative Commons Attribution](http://creativecommons.org/licenses/by/4.0/).
