---
layout: page
title: VMC Protocol
lang: en
---

# VMC Protocol

This feature supports [VMC Protocol](https://protocol.vmc.info/) (VMCP) to send or receive pose / facial data with other applications which support VMC Protocol.

<div class="row">
{% include docimg.html file="/images/docs/vmcp_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### Note: Before using VMC Protocol with VMagicMirror
{: .doc-sec2 }

In VMagicMirror, VMC Protocol is treated as a most advanced **unstable** feature, because there are so many VMCP supported apps.

VMagicMirror's author tests VMCP feature with following apps, though the apps also might be unstable by future updates.

<div class="doc-ul" markdown="1">

- [LuppetX 1.0.5](https://luppet.jp/)
- [VRM Posing Desktop v4.5.8](https://store.steampowered.com/app/1895630/VRM_Posing_Desktop/)

</div>

Especially VMCP data communication from other devices (e.g. app running on smart phone) can be more unstable and leads higher network load, than receiving data from other apps in PC.

### Setup: Enable VMC Protocol Tab
{: .doc-sec1 }

By default VMCP feature is hidden in control panel window.

To enable it, open setting window and select `VMCP` tab > `Show VMCP Tab on Main Window` to show settings UI on main control panel window.

<div class="row">
{% include docimg.html file="/images/docs/vmcp_enable.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/vmcp_settings.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

Following steps are available in `VMC Protocol` tab in control panel window.

### Receive Data

This section is about how to setup data receive settings.

#### Basic Usage
{: .doc-sec2 }

Check `Enable VMC Protocol` to enable the feature.

Setup data source and click `Apply` to apply changes. 

<div class="doc-ul" markdown="1">

- Port number
- Data to apply
  - Upper Body
  - Hand
  - Leg
  - Facial (BlendShape)
- App Name: this is just for memo area and does not have effect how app will behave.

</div>

`Status` area indicates check mark during receicing VMCP data.

Note that VMagicMirror recommends to load the same avatar both in VMCP source application and VMagicMirror.

<div class="note-area" markdown="1">

**NOTE**

VMagicMirror does a kind of re-target process when apply the motion.

You can use different avatars in source app, as far as it does not lead appearance issue.

</div>


#### Advanced Settings
{: .doc-sec2 }

Advanced Settings support detailed option. In most cases you should use default option values.

<div class="doc-ul" markdown="1">

- `Smooth Received Pose`: Enable to smooth received pose. Consider to enable this option when avatar's motion seems jerky.
- `Apply Base Spine Bones Tracking Result`: Enable to apply tracking result by VMagicMirror onto VMC Protocol based pose. This option is useful when receiving fixed pose and you want to apply additional motion to the avatar.

</div>

### Send Data
{: .doc-sec1 }

In v4.0.0 and later version, VMagicMirror supports to send data with VMC Protocol.

Check `Enable VMC Protocol Data Send` to enable data send.


#### Limitations by Edition
{: .doc-sec2 }

In Standard Edition, special post-process effect will be applied during `Enable VMC Protocol Data Send` is on. Also, Standard Edition cannot send data when `Game Locomotion` is selected for body motion style option.

Note that the post-process effect does nothing to do with data to send. If the visual effect is not bothering for your use case, then there are no problem to send data by Standard Edition.

If you need to use the feature without limitation please get Full Edition. See detail at [Download](../../download).


#### Data Send Settings
{: .doc-sec2 }

To send the data, specify `Target Address` and `Target Port`, then select `Apply Changes`.

There are several options available.

<div class="doc-ul" markdown="1">

- `Show Effect`: In Full Edition, turn on this to apply visual effect during data send is active. In Standard Edition, this setting cannot be changed.
- `Send Bone Poses`: Turn on to send avatar's bone poses.
- `Send Finger Bone Poses`: Turn on to send avatar's finger bone poses. If receiver app only needs body poses like foot and arm, then turn off this to reduce data send size.
- `Send BlendShape Data`: Turn on to send BlendShape facial data.
- `Send Custom BlendShape`: Turn on to send all BlendShape values. If receive app uses only standard BlendShape defined in VRM standard, turn off this to omit unused facial data.
- `Convert to VRM0.x BlendShape Name`: If receiver app needs VRM 0.x based BlendShape name, turn on this option to convert specific BlendShape names.
- `Limit Data Rate to 30/sec`: Use this option to limit data send rate while VMagicMirror is running in 60FPS.

</div>


