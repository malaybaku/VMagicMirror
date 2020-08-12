---
layout: page
title: Use Perfect Sync for External Tracking
permalink: /en/tips/use_vroid_hub
lang_prefix: /en/
---

[Japanese](../../tips/perfect_sync)

# Tips: Use Perfect Sync for External Tracking

Perfect Sync is supported from v1.3.0.

#### What is Perfect Sync?
{: .doc-sec2 }

"Perfect Sync" is the feature to achieve rich expression using [External Tracking](../docs/external_tracker). The feature is based on External Tracking, so it requires Face ID supported iPhone or iPad.

Please see [External Tracking](../docs/external_tracker) beforehand, to understand how to use the system.

From the technical view, Perfect Sync maps the blendshapes from iOS ARKit FaceTracking to VRM's BlendShapeClips.

　

#### Try Perfect Sync
{: .doc-sec2 }

*If you are hurry to go into how to model setup, you can skip this section.

There are 2 ways to try Perfect Sync right now.

The first way is to load ready-to-use model.

Download [千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853), and load on VMagicMirror. Then, open `Ex.Tracker` tab and connect to the iOS app. Then turn on `Use Perfect Sync` check.

(TODO: screenshot)

That's it! Try move your eyebrow, mouth, or tongue as you want.

(TODO: screenshot)

Also, you can try with other model which supports [Vear](https://apps.apple.com/jp/app/id1490697369) Perfect Sync. The model supporting Vear Perfect Sync will also work on VMagicMirror. (Please see the bottom section on this page for the detail.)


The second way is to use VRoid Studio model. This way is available if the model is VRoid Studio based, and you do not add any Blendshape to model mesh.

Load the model, and open `Ex.Tracker` tab and connect to the iOS app. Then turn on `Use Perfect Sync`, `Use VRoid Default Setting` check. now it is ready!

(TODO: screenshot)

This way is handy but the expression is not so rich. For example, this way does not support tongue out, cheek puff etc.


#### Setup Step1. Prepare model BlendShapes
{: .doc-sec2 }

This section is about how to get ready your VRM to perfect sync.

The first step is to create 52 blendshapes corresponding to iOS ARKit FaceTracking. This requires some CCD tool like Blender.

Please see the variation of the blendshape and how the mesh should move on each blendshape in the following blog post. (In Japanese, but you will be able to read with some translation tools.)

https://hinzka.hatenablog.com/entry/2020/06/15/072929

If you want to see the example, [千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853) model will be the help.

Note that, the blog above shows all of 52 blendshapes, but you can skip some of them if you feel they are too subtle.




#### Setup Step2. Create BlendShapeClip on Unity
{: .doc-sec2 }

Import the model with blendshapes created in previous step. Then, add BlendShapeClips for each blendshape. Please set the names of the BlendShapeClip as following.

`BrowInnerUp`
`BrowDownLeft`
`BrowDownRight`
`BrowOuterUpLeft`
`BrowOuterUpRight`

`EyeLookUpLeft`
`EyeLookUpRight`
`EyeLookDownLeft`
`EyeLookDownRight`
`EyeLookInLeft`
`EyeLookInRight`
`EyeLookOutLeft`
`EyeLookOutRight`

`EyeBlinkLeft`
`EyeBlinkRight`
`EyeSquintRight`
`EyeSquintLeft`
`EyeWideLeft`
`EyeWideRight`

`CheekPuff`
`CheekSquintLeft`
`CheekSquintRight`

`NoseSneerLeft`
`NoseSneerRight`

`JawOpen`
`JawForward`
`JawLeft`
`JawRight`

`MouthFunnel`
`MouthPucker`
`MouthLeft`
`MouthRight`
`MouthRollUpper`
`MouthRollLower`
`MouthShrugUpper`
`MouthShrugLower`
`MouthClose`

`MouthSmileLeft`
`MouthSmileRight`
`MouthFrownLeft`
`MouthFrownRight`
`MouthDimpleLeft`
`MouthDimpleRight`
`MouthUpperUpLeft`
`MouthUpperUpRight`
`MouthLowerDownLeft`
`MouthLowerDownRight`
`MouthPressLeft`
`MouthPressRight`
`MouthStretchLeft`
`MouthStretchRight`

`TongueOut`

Basically each BlendShapeClip should use only one Blendshape created in previous step, and its `weight` will be `100`.

If you do not create blendshape in Step 1 for some BlendShapeClip, then leave those BlendShapeClip content empty, which means the clip actually does nothing.

(TODO: screenshot)


#### Note. The background of Vear compatibility
{: .doc-sec2 }

The requirement for Perfect Sync in VMagicMirror is almost same as [Vear](https://apps.apple.com/jp/app/id1490697369). This is based on 2 background,

First, Perfect Sync's specification comes from reference models and blog posts by [Hinzka](https://twitter.com/hinzka). Those resources also make direction for Vear, so the specifications are similar.

Second background is that, VMagicMirror aims to reduce the user task of model re-setup, by make model specification almost same.

However, note that VMagicMirror does not ensure perfect compatibility to Vear, about Perfect Sync.
