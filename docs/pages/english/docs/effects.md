---
layout: page
title: Effects
permalink: /en/docs/effects
lang_prefix: /en/
---

[Japanese](../../docs/window)

# Effects

`Effects` tab supports whole image quality, light, shadow, bloom and wind settings.

<div class="row">
{% include docimg.html file="/images/docs/effects_top_1.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/effects_top_2.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### 1. Settings
{: .doc-sec2 }

`Quality`: Select whole image quality.

`Light`: Color, intensity, and direction of light.

`Shadow`: Intensity and direction of shadow.

`Bloom`: Color and intensity of bloom.

`Wind`: Strength and direction of wind.

#### 2. Hint
{: .doc-sec2 }

Light and shadow has separated orientations, so you can set the light orientation simply for the avatar's looking, while adjust shadow orientation to show it on the back of the avatar.

Also you can adjust the depth offset and orientation of the shadow to , so that your avatar looks near or far to the screen.

Below is default setting, and example setting to show distance between screen and the character.

<div class="row">
{% include docimg.html file="/images/docs/shadow_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/shadow_look_far.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

Please be aware that some VRM character uses `Unlit` type shader, to which the light setting has no effect.

For the wind settings:

1. Please setup `VRMSpringBone` beforehand, to enable wind-based motion.
2. Wind feature moves all `VRMSpringBone` components, so "only hair (not skirt)" like setting is not supported.
