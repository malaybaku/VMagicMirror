---
layout: page
title: Effects
lang: en
---

# Effects

`Effects` tab supports whole image quality, light, shadow, bloom, outline, rim light, wind and other settings.

<div class="row">
{% include docimg.html file="/images/docs/effects_top_1.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/effects_top_2.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### Image Quality
{: .doc-sec2 }

Set image quality and framerate.

<div class="doc-ul" markdown="1">

- `Quality`: Select whole rendering quality. Higher quality improves appearance, while GPU load becomes higher.
- `Framerate`: Set rendering framerate. Select `Match to Monitor's Refresh Rate` to render according to monitor setting.
- `Anti Alias`: Choose anti alias (Multisample Anti-Alias) option. Higher setting makes jagged edges less noticeable, while rendering load becomes higher. The feature is off by default.
- `Low FPS for Bone Motion`: Turn on to reduce avatar motion's FPS. Note that this option does not reduce CPU usage.
- `Disable HDR even when Quality is High or higher`: Turn on to render without HDR even when quality is High or higher. Turning off HDR saves performance, while lighting effect quality becomes lower.

</div>


#### Light
{: .doc-sec2 }

Set the light applied to the avatar.

<div class="doc-ul" markdown="1">

- `Light Color`: Set the color of the light applied to the avatar.
- `Intensity [%]`: Set light intensity. Larger value makes the avatar brighter.
- `Yaw Angle [deg]`: Set horizontal direction of the light.
- `Pitch Angle [deg]`: Set vertical direction of the light.
- `Desktop Color Based Lighting`: Capture the color tone of PC screen in real time and apply it to the avatar light color. Use this to blend avatar appearance with the screen color.

</div>

<div class="note-area" markdown="1">

**NOTE**

`Desktop Color Based Lighting` option uses a kind of window capture API and it leads yellow frame effect on your monitor.

</div>


#### Shadow
{: .doc-sec2 }

Set the avatar's shadow.

<div class="doc-ul" markdown="1">

- `Enable Shadow`: Turn on to show the avatar's shadow. When this is off, both regular shadow and Full Body Shadow are hidden.
- `Shadow Color`: Set shadow color. Usually, near-black color works well.
- `Intensity [%]`: Set shadow intensity. Larger value makes the shadow darker.
- `Blur Size`: Set blur size of regular shadow. Larger value makes shadow edges softer. This setting does not affect Full Body Shadow.
- `Yaw Angle [deg]`: Set horizontal direction where the shadow extends.
- `Pitch Angle [deg]`: Set vertical direction where the shadow extends.
- `Depth Offset [cm]`: Adjust perceived depth of the shadow. Use this to adjust distance impression between avatar and background.
- `Full Body Shadow Settings`: Set the shadow shown around avatar's feet. This shadow is shown at different position from regular shadow, and is designed to achieve good appearance when showing avatar's full body.
- `Always Apply Full Body Shadow`: Turn on to show Full Body Shadow regardless of lower body motion state.
- `Apply When Locomotion is Active`: Turn on to show Full Body Shadow when the app detects lower body motion is active. By default, lower body motion is considered active when [Game Input](../game_input) is enabled, or when lower body pose is received by [VMC Protocol](../vmc_protocol).

</div>


#### Bloom
{: .doc-sec2 }

Bloom is an effect to add soft light around the avatar.

<div class="doc-ul" markdown="1">

- `Enable Effect`: Turn on to enable Bloom. When this is off, Bloom is not applied regardless of the `Intensity [%]` value.
- `Bloom Color`: Set bloom color.
- `Intensity [%]`: Set bloom intensity. When this value is 0, Bloom is not visible even if `Enable Effect` is on.
- `Threshold [%]`: Set brightness threshold to apply bloom. Larger value applies bloom only to brighter areas.

</div>


#### Outline
{: .doc-sec2 }

Outline is an effect to draw lines around the avatar.

<div class="doc-ul" markdown="1">

- `Enable Outline Effect`: Turn on to enable outline effect. Outline effect is applied only when `Transparent Window` is enabled.
- `Outline Color`: Set outline color.
- `Thickness`: Set outline width.
- `High Quality Mode`: Turn on to improve outline quality. This requires higher GPU resources.

</div>

Note that, when there is almost-opaque visual including shadow, bloom, and accessories, outline will also be applied to those elements.


#### Rim Light
{: .doc-sec2 }

Rim Light is available in v5.0.0 and later versions. It is an effect to add light around the avatar's outline. Use this when you want the avatar to stand out from the background.

<div class="doc-ul" markdown="1">

- `Enable Effect`: Turn on to enable Rim Light.
- `Rim Light Color`: Set Rim Light color.
- `Emission`: Set emission intensity of Rim Light. Larger value makes it look brighter when used with HDR and Bloom.
- `Intensity [%]`: Set how strongly Rim Light is applied. When this value is 0, Rim Light is not visible even if `Enable Effect` is on.
- `Thickness`: Set width of Rim Light. When this value is 0, Rim Light is not visible even if `Enable Effect` is on.
- `Angle [deg]`: Set direction where Rim Light appears strongly.

</div>


#### Wind
{: .doc-sec2 }

Add wind-like motion to avatar's hair, clothes, and other spring bone parts.

<div class="doc-ul" markdown="1">

- `Enable Wind`: Turn on to enable wind effect.
- `Strength [%]`: Set strength of wind-based motion.
- `Fineness [%]`: Set fineness of wind variation. Larger value makes the wind change more finely.
- `Direction [deg]`: Set wind direction.

</div>

For the wind settings:

<div class="doc-ul" markdown="1">

1. Please setup `VRMSpringBone` beforehand, to enable wind-based motion.
2. Wind feature moves all `VRMSpringBone` components, so "only hair (not skirt)" like setting is not supported.

</div>


#### Ambient Occlusion
{: .doc-sec2 }

Ambient Occlusion is an effect to darken dents and boundaries around avatar parts. Use this to emphasize three-dimensional appearance.

<div class="doc-ul" markdown="1">

- `Enable Effect`: Turn on to enable Ambient Occlusion.
- `Color`: Set the color used for Ambient Occlusion. Usually, a near-black color works well. A color near white makes the effect less noticeable.
- `Intensity [%]`: Set Ambient Occlusion intensity. Larger value makes darkened areas more noticeable.

</div>

#### Hint
{: .doc-sec2 }

Light and shadow have separated orientations, so you can set the light orientation simply for the avatar's looking, while adjust shadow orientation to show it on the back of the avatar.

Also, you can adjust the depth offset and orientation of the shadow, so that your avatar looks near or far to the screen.

Below is default setting, and example setting to show distance between screen and the avatar.

<div class="row">
{% include docimg.html file="/images/docs/shadow_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/shadow_look_far.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

Please be aware that some VRM avatars use `Unlit` type shader, to which the light setting has no effect.
