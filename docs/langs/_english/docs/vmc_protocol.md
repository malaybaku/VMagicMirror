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
- [WebcamMotionCapture 1.9.0](https://webcammotioncapture.info/)

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
  - Head: Head pose.
  - Hand: Hand and finger pose.
  - Facial: Face Blendshape values.
- App Name: this is just for memo area and does not have effect how app will behave.

</div>

<div class="note-area" markdown="1">

**NOTE**

When VMC Protocol's hand pose receive is active, VMagicMirror's default motion features (e.g. keyboard typing motion) does not work.

Exception is [Word to Motion](./expressions), which runs with higher priority than VMCP based motion.

</div>

`Status` area indicates check mark during receicing VMCP data.

VMagicMirror recommends to load the same avatar both in VMCP source app and VMagicMirror.

<div class="note-area" markdown="1">

**NOTE**

VMagicMirror does a kind of re-target process when apply the motion.

There are no problems to load different avatar in source app, as far as it leads appearance issue.

</div>


#### Advanced Settings
{: .doc-sec2 }

Advanced Settings support detailed option. In most cases you should use default option values.

<div class="doc-ul" markdown="1">

- `Apply received bone pose without any adjust`: Turn on to apply VMCP based bone pose as-is. Enable this option especially when avatar's arm has bad appearance.
- `Disable Camera feature during VMCP is active`: Turn on to disable webcam using features in VMagicMirror during VMCP is active. This option is on by default. You should enable this option if VMCP source app uses your PC's webcam.

</div>


#### Known Issues
{: .doc-sec2 }

VMagicMirror v3.3.1 has following known issue.

<div class="doc-ul" markdown="1">

- When `Apply received bone pose without any adjust` is on, some of Word to Motion's motion does not work (e.g. nodding, clapping).

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


