---
layout: page
title: Perfect Sync
lang: en
---

# Tips: Perfect Sync
{: .no_toc}

Perfect Sync is supported from v1.3.0.

<div class="toc-area" markdown="1">

#### Content
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### What is Perfect Sync?
{: .doc-sec2 }

"Perfect Sync" is an advanced feature of [External Tracking](../../docs/external_tracker), to achieve rich facial expressions. The feature is based on External Tracking, so it requires Face ID supported iPhone or iPad. VRM model also needs specialized setup for Perfect Sync.

Please see [External Tracking](../../docs/external_tracker) beforehand, to understand how to use the system.

From the technical view, Perfect Sync maps the all blendshapes obtained by iOS ARKit FaceTracking to VRM's BlendShapeClips.


#### Other tools supporting Perfect Sync
{: .doc-sec2 }

Perfect Sync is not a unique feature of VMagicMirror.

For example, [Vear](https://apps.apple.com/jp/app/id1490697369) and [Luppet](https://luppet.appspot.com/) also support it. These tools have basically same requirement for the model. 

So when you set up the model once, the model will be available on all of those tools.


#### Try Perfect Sync
{: .doc-sec2 }

*If you already know how perfect sync works with other applications (like Vear), you can skip this section.

There are 2 ways to try Perfect Sync right now.

The first way is to load ready-to-use model.

Download [千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853), and load on VMagicMirror. Then, open `Ex.Tracker` tab and connect to the iOS app. Then turn on `Use Perfect Sync` check.

That's it! Try move your eyebrow, mouth, or tongue as you want.

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_setup_model_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Also, you can try other models which work on [Vear](https://apps.apple.com/jp/app/id1490697369) Perfect Sync. The model working on Vear Perfect Sync will also work on VMagicMirror. (*Please see the bottom section about compatibility.)


The second way is to use VRoid Studio based model with [HANA_Tool](https://booth.pm/ja/items/2437978).

If your model is enough plane (enough few modification after exported from VRoid Studio), the tools works what needed for perfect sync.

For the usage of `HANA_Tool` please check it by yourself.

ref: [クリックで実装！パーフェクトシンク　BY HANA Tool](https://hinzka.hatenablog.com/entry/2020/10/12/014540)


<div class="note-area" markdown="1">

**NOTE** 

VMagicMirror v1.6.2 and older version had `Use VRoid Default Setting` option.

However, this feature was unstable against the difference of VRoid Studio version, so the feature has been removed at v1.7.0.

Now `Use VRoid Default Setting` is not recommended even if you are using VMagicMirror v1.6.2 or older version.

</div>


#### Setup Step1. Prepare model BlendShapes
{: .doc-sec2 }

This section is about how to get ready your VRM to perfect sync.

The first step is to create 52 blendshapes corresponding to iOS ARKit FaceTracking. This requires some CCD tool like Blender.

Please see the variation of the blendshape and how the mesh should move on each blendshape in the following blog post. (In Japanese, but you will be able to read with some translation tools.)

[iPhoneトラッキング向けBlendShapeリスト (The BlendShape list for iPhone Face Tracking)](https://hinzka.hatenablog.com/entry/2020/06/15/072929)

If you need an example, [千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853) model will be the help.

Note that, the blog above shows all of 52 blendshapes, but you can skip some of them if you feel they are too subtle.

#### Setup Step2. Create BlendShapeClip on Unity
{: .doc-sec2 }

Import the model, and add following 52 BlendShapeClips. Please set the names of the BlendShapeClip as following.

<div class="doc-ul" markdown="1">

- `BrowInnerUp`
- `BrowDownLeft`
- `BrowDownRight`
- `BrowOuterUpLeft`
- `BrowOuterUpRight`
- `EyeLookUpLeft`
- `EyeLookUpRight`
- `EyeLookDownLeft`
- `EyeLookDownRight`
- `EyeLookInLeft`
- `EyeLookInRight`
- `EyeLookOutLeft`
- `EyeLookOutRight`
- `EyeBlinkLeft`
- `EyeBlinkRight`
- `EyeSquintRight`
- `EyeSquintLeft`
- `EyeWideLeft`
- `EyeWideRight`
- `CheekPuff`
- `CheekSquintLeft`
- `CheekSquintRight`
- `NoseSneerLeft`
- `NoseSneerRight`
- `JawOpen`
- `JawForward`
- `JawLeft`
- `JawRight`
- `MouthFunnel`
- `MouthPucker`
- `MouthLeft`
- `MouthRight`
- `MouthRollUpper`
- `MouthRollLower`
- `MouthShrugUpper`
- `MouthShrugLower`
- `MouthClose`
- `MouthSmileLeft`
- `MouthSmileRight`
- `MouthFrownLeft`
- `MouthFrownRight`
- `MouthDimpleLeft`
- `MouthDimpleRight`
- `MouthUpperUpLeft`
- `MouthUpperUpRight`
- `MouthLowerDownLeft`
- `MouthLowerDownRight`
- `MouthPressLeft`
- `MouthPressRight`
- `MouthStretchLeft`
- `MouthStretchRight`
- `TongueOut`

</div>

Basically each BlendShapeClip should use only one Blendshape created in previous step, and its `weight` will be `100`.

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_clip_setting_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

If you do not create blendshape in Step 1 for some BlendShapeClip, then leave those BlendShapeClip content empty, which means the clip actually does nothing.

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_empty_clip_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

After the setup, export the VRM.


#### References
{: .doc-sec2 }

See the following blog posts (in Japanese), to check the techniques for perfect sync model setup.

[iPhoneトラッキング向けBlendShapeリスト (The BlendShape list for iPhone Face Tracking)](https://hinzka.hatenablog.com/entry/2020/06/15/072929)

[VRoidでかんたん！パーフェクトシンク（1/3）VRoidモデルのFBXエクスポート (VRoid Perfect Sync (1/3) Export FBX)](https://hinzka.hatenablog.com/entry/2020/08/15/145040)

[パーフェクトシンクであそぼう！ (Enjoy Perfect Sync!)](https://hinzka.hatenablog.com/entry/2020/08/15/145040)

[パーフェクトシンクの顔をお着換えモデルに移植しよう (Copy perfect sync data to cloth changed model)](https://hinzka.hatenablog.com/entry/2020/08/17/001851)

　

#### Note. The background of Vear compatibility
{: .doc-sec2 }

The requirement for Perfect Sync in VMagicMirror is almost same as [Vear](https://apps.apple.com/jp/app/id1490697369). This is based on 2 background,

First, Perfect Sync's specification comes from reference models and blog posts by [Hinzka](https://twitter.com/hinzka). Those resources also make direction for Vear, so the specifications are similar.

Second background is that, VMagicMirror aims to reduce the user task of model re-setup, by make model specification almost same.

However, note that VMagicMirror does not ensure perfect compatibility to Vear, about Perfect Sync.
