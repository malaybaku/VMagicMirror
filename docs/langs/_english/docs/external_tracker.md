---
layout: page
title: External Tracker App
lang: en
---

# Exteral Tracker App
{: .no_toc }

`VMagicMirror` v1.1.0 and later supports external tracking apps to move the avatar.

Currently only iOS application is supported.

<div class="toc-area" markdown="1">

#### Content
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

### Advantage and Limitation
{: .doc-sec1 }

Please check following points about external tracker app.

#### Advantages
{: .doc-sec2 .no_toc }

<div class="doc-ul" markdown="1">

- **Precise Tracking**: Much more precise than VMagicMirror's webcam based tracking.
- **Less CPU usage**: Heavy task of face tracking does not run anymore on PC (, instead iOS device take it).
- **Can Detach Webcam**: You can detach webcam if you only use this feature. This will prevent unexpected face exposure to others, when your screen is shared.

</div>

#### Limitations
{: .doc-sec2 .no_toc }

If you want to use iOS device, it must be enough new iPhone or iPad, which supports Face ID or has A12 Bionic chip (or newer chip).

<div class="doc-ul" markdown="1">

- [Supported Models](https://support.apple.com/en-us/HT209183)
- [iPad Models("See All Models" will show all models' chip)](https://www.apple.com/ipad/compare/)
- [iPhone Models("See All Models" will show all models' chip)](https://www.apple.com/iphone/compare/)

</div>

Also you have option to use Android device.


Also please be aware of following points.

<div class="doc-ul" markdown="1">

- **Less Stable**: Less stable than conventional webcam based tracking. This is because of the inter-device LAN communication.
- **Risk of Bug by App Update**: If iOS application have some update, it maybe lead issue.
- **A Bit Complicated Setup**: This feature involve a bit difficult setup, because of inter-device communication.

</div>

### Preparation
{: .doc-sec1 }

For the preparetion check following:

1. PC and the tracking device (iPhone/iPad) are in the same network (LAN).
2. LAN environment is stable.
3. There is a iPhone / iPad stand, so that the device can capture your face via front camera.
4. If you wear glasses put off, or use one with thin frame.

Glasses maybe leads less eye move tracking preciseness, including blink tracking.


### How to Setup
{: .doc-sec1 }

Select `Ex Tracker` tab on the control panel, then turn on `Enable External Tracker`.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_00_enable_feature.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

By checking this, VMagicMirror become ready to connect with external apps.

### How to Setup each App
{: .doc-sec1 }

Please see per-app specific setup process.

iOS: **[iFacialMocap](../external_tracker_ifacialmocap)**

(*This area will be updated when other app supported.)

<div class="note-area" markdown="1">

**NOTE**

Help about Android App [MeowFace](https://play.google.com/store/apps/details?id=com.suvidriel.meowface) has been removed, since the it  seems not work correctly anymore.

</div>



### Supported Options 
{: .doc-sec1 }

<div class="doc-ul" markdown="1">

- `Apply LipSync using External Tracker Data`
- `Disable horizontal flip`: Enable to turn off horizontal flip process.
- `Enable Forward/Backward Move`: Turn on to allow move avatar forward and backward(*).
- `Use Perfect Sync`: Enable Perfect Sync. See detail at [Perfect Sync Tips](../../tips/perfect_sync).

</div>

*`Enable Forward/Backward Move` option does almost nothing if the avatar touches virtual keyboard, gamepad etc. To use this option, confirm that `Standing Only` option is selected at `Streaming` tab  > `Motion` > `Body Motion Style`.

`Apply LipSync using External Tracker Data` feature turns off microphone based lipsync, which leads less CPU load on the PC.

There is also another feature that, the external app gets mouth shape by camera, so your motion will be reflected even when your mouth moves silently.

On the other hand, the tracking precision decreases when the device cannot see your mouth (mainly because of mic or hop guard).

In this case turn off `Apply LipSync using External Tracker Data` to use conventional microphone based lipsync.
