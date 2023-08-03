---
layout: page
title: VMC Protocol
lang: en
---

# VMC Protocol

This feature is available from v4.0.0

In VMCP tab, you can use [VMC Protocol](https://protocol.vmc.info/) (VMCP) to receive pose and facial data from other applications which support VMC Protocol.

(TODO: この画像をVMCPタブの画像に変更)

<div class="row">
{% include docimg.html file="/images/docs/devices_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### 注意: VMC Protocolを使う前に
{: .doc-sec2 }

In VMagicMirror, VMC Protocol is treated as a most advanced **unstable** feature, because there are so many VMCP supported apps.

VMagicMirror's author tests VMCP feature with following apps, though the apps also might be unstable by future updates.

<div class="doc-ul" markdown="1">

- [LuppetX 1.0.5](https://luppet.jp/)
- [WebcamMotionCapture 1.9.0](https://webcammotioncapture.info/)
- (TODO: スマホも何かはほしい、安定性に注意して選びたい)

</div>

Especially VMCP data communication from other devices (e.g. app running on smart phone) can be more unstable than receiving data from other app in PC.


#### 1. Basic Usage
{: .doc-sec2 }

Turn on `Enable VMC Protocol` to enable the feature.

Then, setup data source and click `Apply` to apply changes.

<div class="doc-ul" markdown="1">

- Port number
- Data to apply (Head, Hand, Facial)

</div>

<div class="note-area" markdown="1">

**NOTE**

During VMC Protocol's hand pose receive is active, VMagicMirror's default motion features (e.g. keyboard typing motion) does not work.

Exception is [Word to Motion](./expressions), which runs with higher priority than VMCP based motion.

</div>

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
- `Disable Mic feature during VMCP facial is active`: Turn on to disable microphone recording process during VMCP based facial is active. The option is on by default, and even the option is off you cannot apply VMM's lipsync over the VMCP based facial.

</div>
