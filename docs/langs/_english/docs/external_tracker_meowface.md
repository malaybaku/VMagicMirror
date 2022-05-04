---
layout: page
title: Connect to MeowFace
permalink: /en/docs/external_tracker_meowface
lang: en
---

[Japanese](../../docs/external_tracker_meowface)

# Connect to MeowFace
{: .no_toc }

Show how to setup MeowFace for [External Tracker App](./external_tracker).


### What is MeowFace?
{: .doc-sec1 }

MeowFace is a free Android app, to mime iOS's face tracking function.

Especially it supports same data protocol as [iFacialMocap](./external_tracker_ifacialmocap).

In VMagicMirror, you can connect MeowFace as if it is iFacialMocap.

The device requirement is not so clear, but you can check it by just download and try by yourself.

The app is available in Google Play.

[MeowFace](https://play.google.com/store/apps/details?id=com.suvidriel.meowface)


### What is Supported
{: .doc-sec1 }

MeowFace aims to mime iOS's face tracking, but some of the feature is not supported.

<div class="doc-ul" markdown="1">

Supported:

- Head rotation tracking
- Facial basic parameters tracking (blink, mouth motion)
- [Perfect Sync](../tips/perfect_sync)

Not Supported:

- Head motion tracking
- Some facial parameter's tracking

</div>

Also note that, you will need to adjust facial input parameters in MeowFace and the process might be a bit difficult.


### Connect to VMagicMirror
{: .doc-sec1 }

Start MeowFace and see the device's IP address at the top.

Put the device where the device's camera can capture your face.

Go to PC and `Ex Tracker` tab > `Connect to App` > select `iFacialMocap`.

Then input the IP address shown in iOS device, and click `Connect` to complete connection.

<div class="note-area" markdown="1">

**IMPORTANT** 

If you seem to fail to connect, please see below [iFacialMocap troubleshoot](./external_tracker_ifacialmocap#troubleshoot), especially Q1, Q2 and Q3.

Though the apps are different, the connection failure causes are almost common.

Also please check if an anti-virus software disturbs connection between PC and iOS device.

</div>

If your avatar looks wrong orientatoin or face motion does not start, execute `Cralibrate Face Pose` to calibrate.


### Adjust Facial Parameters
{: .doc-sec1 }

When avatar's facial control is not going well, see MeowFace and scroll facial parameters, to adjust `Weight` and `Max` values.

For example, blink parameters are shown as `eyeBlink_L` and `eyeBlink_R`.

If you close the eyes but avatar does not, then increase `Weight` for them.

<div class="note-area" markdown="1">

**NOTE**

MeowFace is much newer app than iFacialMocap, and there might be further updates. Please check the app newest how-to when needed.

</div>
