---
layout: page
title: Download
permalink: /en/download
lang_prefix: /en/
---

[Japanese](../download)

# Download

VMagicMirror is Available on [BOOTH](https://booth.pm/ja/items/1272298).

<a target="_blank" href="https://baku-dreameater.booth.pm/items/1272298/">
  <img src="https://asset.booth.pm/static-images/banner/468x60_02.png">
</a>

Please see  [License](./about_license.html) if you are not clear the permitted usage.
You can download source code on [GitHub](https://github.com/malaybaku/VMagicMirror).

For v1.8.0 and later, there are two edition, and 4 options to get them.

Editions:

<div class="doc-ul" markdown="1">

- Standard Edition: Basic edition, with almost all feature available.
- Full Edition: Edition without restriction on hand tracking. Please see next section for detail.

</div>

How to get:

<div class="doc-ul" markdown="1">

- BOOTH free version: This is Standard Edition, and most basic way to get VMagicMirror.
- BOOTH boost version: This is Stadard Edition. Please take it as pure donation.
- BOOTH full version: This is Full Edition. Please see next section for detail.
- Fanbox: Full Edition is available for each update. Please see next section for detail.

</div>

### Difference between Standard / Full Edition
{: .doc-sec1 }

v1.8.0 and later version have two editions, Standard Edition and Full Edition.

The only difference between the editions is the behavior during hand tracking.

In Standard Edition, you will see the special post-process effect during image based hand tracking enabled.
Full Edition does not have those restriction, so that you can use hand tracking feature completely.

(TODO: エフェクトがかかった状態のスクリーンショット)

All other features works same in both editions.

In most cases standard edition will be enough for what you need.
When you consider to get the full edition, please understand that the price assumes the value of rather the whole software than hand tracking feature itself. 

Of course there are additional meaning like sharewere / donation, and they will be powerful motivation for the app maintenance.


### Where to get Full Edition
{: .doc-sec1 }

There are 2 options to get full edition.

<div class="doc-ul" markdown="1">

- [BOOTH](https://booth.pm/ja/items/1272298) full edition : Once you get, all future updates are also available.
- [Fanbox](https://baku-dreameater.fanbox.cc/) paid plan : You can get updates for each version.

</div>

Note:

<div class="doc-ul" markdown="1">

- If you select fanbox option and quit later, then further updates are no more available.
- In both option the update is available at same time. In other words, fanbox is not a way to early access.

</div>


### Environments
{: .doc-sec1 }

#### Necessary
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Windows 10 64bit. Windows 8.1 or previous OS, and 32bit edition might not work.

</div>

#### Optional
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Microphone: Lipsync (viseme) supports physical micrphone and also virtual input. You can use voice changer output as lipsync input.
- Web camera: Please layout to capture your whole face. No high resolution needed, as VMagicMirror use face image with low resolution (320 x 240).
- Gamepad: Support XInput style gamepad. XBox controller is a popular one supported. Also you can use DUAL SHOCK 4.
- iPhone / iPad: Please see [External Tracker App](./docs/external_tracker) for the detail.
- MIDI Controller: Recommend a controller mainly with key input.

</div>

#### Unsupported Environments
{: .doc-sec2 }

Following environment might leads problem.

<div class="doc-ul" markdown="1">

- Old AMD CPU may crash the program on startup.
- When using old GPU the face texture might not be loaded.

</div>

#### Checked Environments
{: .doc-sec2 }

The developer checks VMagicMirror performance on the following environments. Please contact if there is bug-like performance issue.


**1: Desktop PC**

<div class="doc-ul" markdown="1">

- CPU: Intel Core i7-6700K
- GPU: GeForce GTX 1080
- Webcam: C922 Pro Stream Webcam
- Microphone: 
    - Output from VoiceMeeter Banana
    - VT-4 WET (Voice changed VT-4 Output)
    - C922 Pro Stream Webcam

</div>

**2: Laptop PC(Surface Book 2)**

<div class="doc-ul" markdown="1">

- Webcam: Embedded front camera
- Microphone: Embedded microphone

</div>
