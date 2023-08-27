---
layout: page
title: VMC Protocol
lang: en
---

# VMC Protocol

This feature is available from v4.0.0

In VMCP tab, you can use [VMC Protocol](https://protocol.vmc.info/) (VMCP) to receive pose and facial data from other applications which support VMC Protocol.

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


#### 1. Basic Usage
{: .doc-sec2 }

By default VMCP feature is hidden in control panel window.

To enable it, open setting window and select `VMCP` tab > `Show VMCP Tab on Main Window` to show settings UI on main control panel window.

<div class="row">
{% include docimg.html file="/images/docs/vmcp_enable.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

Then, select `VMCP` tab in control panel window.

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


#### 2. Advanced Settings
{: .doc-sec2 }

Advanced Settings support detailed option. In most cases you should use default option values.

<div class="doc-ul" markdown="1">

- `Disable Camera feature during VMCP is active`: Turn on to disable webcam using features in VMagicMirror during VMCP is active. This option is on by default. You should enable this option if VMCP source app uses your PC's webcam.

</div>
