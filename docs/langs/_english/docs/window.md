---
layout: page
title: Window
lang: en
---

# Window

`Window` tab supports BG color when the avatar window is not transparent, and also can toggle whether the window is always foreground or not.

<div class="row">
{% include docimg.html file="/images/docs/window_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### Basic Settings
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- `Transparent Window`: Check to make avatar window transparent. Available in Streaming tab. 
- `(When Transparent) Drag Avatar`: Check to enable drag-based move the avatar window when transparent. Available in Streaming tab. 
- `(When Transparent) Always Foreground`: Check to place the avatar almost always foreground. Checked by default.
- `BG Image`: Set or clear background image. 
- `Reset Position`: Press this button to move avatar window just right to the control panel window. Use this function when you lost where the avatar window is.

#### Advanced Settings
{: .doc-sec2 }

- `Background`: Set background color by RGB.
- `Crop`: Setup window crop feature settings. See next section for detail.
- `Spout`: Enable or Disable Spout image output. Spout image can be used from several other apps like OBS Studio with Spout2 Plugin for OBS Studio.
    - `Resolution`: Select the resolution of Spout output. By using fixed resolution, you will get higher resolution image without making avatar window large.
- Transparency settings
    - `Transparent Level`: Select the avatar transparency condition from level 0 to 4. Default value is level 2. Level 0 means always NOT transparent, and level 4 makes avatar always transparent.
    - `Alpha when Transpanrent`: Set the transparency when the avatar is transparent. Higher value means opaque.

#### Crop
{: .doc-sec2 }

Crop feature is available in VMM v4.4.0 and later.

With this feature, the window is cropped by circle or rounded square shape.

<div class="row">
{% include docimg.html file="/images/docs/window_crop.jpg" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="doc-ul" markdown="1">

- `(When Transparent) ENable Crop`: Check `Transparent Window` and this option to enable crop. Available in Streaming tab. 
- `Area Size [%]`: Set the size of cropped area as a percentage of the window size. Default value is `98`.
- `Square Ratio [%]`: Adjust the shape of the cropped area. Set `0` for circle, `100` for square, and intermediate value for rounded square shape. Default value is `0`.
- `Border Width [%]`: Set the border line width as a percentage of the window size. Default value is `1`.
- `Border Color`: Select the color of border.

</div>