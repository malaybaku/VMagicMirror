---
layout: page
title: External Tracker App
permalink: /en/docs/external_tracker
lang_prefix: /en/
---

[Japanese](../../docs/external_tracker)

# Exteral Tracker App

`VMagicMirror` v1.1.0 and later supports external tracking apps to move the avatar.

Currently only iOS application is supported.


#### Advantage and Limitation
{: .doc-sec2 }

Please check following points about external tracker app.

Advantages:

**Precise Tracking**: Much more precise than VMagicMirror's webcam based tracking.

**Less CPU usage**: Heavy task of face tracking does not run anymore on PC (, instead iOS device take it).

**Can Detach Webcam**: You can detach webcam if you only use this feature. This will prevent unexpected face exposure to others, when your screen is shared.

Limitations:

**Device Required**: You need Face ID supported iPhone or iPad. (cf: [Supported Models](https://support.apple.com/en-us/HT209183))

**Less Stable**: Less stable than conventional webcam based tracking. This is because of the inter-device LAN communication.

**Risk of Bug by App Update**: If iOS application have some update, it maybe lead issue.

**A Bit Complicated Setup**: This feature involve a bit difficult setup, because of inter-device communication.

#### Preparation
{: .doc-sec2 }

For the preparetion check following:

1. PC and the tracking device (iPhone/iPad) are in the same network (LAN).

2. LAN environment is stable.

3. There is a iPhone / iPad stand, so that the device can capture your face via front camera.

4. If you wear glasses put off, or use one with thin frame.

Glasses maybe leads less eye move tracking preciseness, including blink tracking.


#### How to Setup
{: .doc-sec2 }

Select `Ex Tracker` tab on the control panel, then turn on `Enable External Tracker`.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_00_enable_feature.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

By checking this, VMagicMirror become ready to connect with external apps.

#### How to Setup each App
{: .doc-sec2 }

Please see per-app specific setup process.

**[iFacialMocap](./external_tracker_ifacialmocap)**

(*This area will be updated when other app supported.)


#### Use LipSync with External Tracking App
{: .doc-sec2 }

Check `Apply LipSync using External Tracker Data`, to utilize mouth motion capture from the external app.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_40_replace_lipsync.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

This feature turns off microphone based lipsync automatically, which leads less CPU load on the PC.

There is also another feature that, the external app gets mouth shape by camera, so your motion will be reflected even when your mouth moves silently.


On the other hand, the tracking precision decreases when the device cannot see your mouth (mainly because of mic or hop guard).

In this case turn off `Apply LipSync using External Tracker Data` to use conventional microphone based lipsync.

#### Perfect Sync

Perfect Sync is an advanced feature supported from v1.3.0, to move the avatar very rich with external tracking.

See the detail at [Use Perfect Sync for External Tracking](../tips/perfect_sync).


#### Face Switch
{: .doc-sec2 }

Face Switch is a feature to switch avatar's face by user expression.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_50_face_switch.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Face switch has parameters to setup.

`Threshold`: Select from 10% to 90%, to specify when the face switch is triggered. Higher value means you have to more clear expression.

`BlendShape`: Choose the BlendShape to apply, or select `(Do Nothing)`(*) as empty selection.

`Keep LipSync`: You can check it for the BlendShape with only eye motion, so the LipSync still work.

*`(Do Nothing)` indication might incorrect appearance as Japanese expression "`(何もしない)`". In this case you can choose `(何もしない)`.

Note that some face expressions are difficult to be recognized.

Also note that, this feature is not an extension of face tracking, but also considerable as shortcut key assignment via your face.

This means irrelevant assignment will be still useful.

For example, you can assign a special face expression for tongue out condition, even if the expression does not include tongue out motion at all.


**NOTE:** `Word to Motion` feature has higher priority. When face switch and `Word to Motion` input runs simultaneously, then `Word to Motion` output is applied.

