---
layout: page
title: Use VRM Animation
lang: en
---

# Tips: Use VRM Animation

This feature is available in v3.4.0 and later version of VMagicMirror.

You can use VRM Animation (VRMA) file to apply customized motion to your avatar, similar to Custom Motion referred at [Use Custom Motion](../use_custom_motion) page.

<div class="note-area" markdown="1">

**NOTE**

This feature is experimental, and has some restrictions. Note that VRMA's specification might change by future updates.
You should keep to use Custom Motion feature if you want to avoid update-related issues.

</div>


#### 1. Target and Limitation
{: .doc-sec2 }

v3.4.0 supports VRMA in [Expressions](../../docs/expressions) feature.

From v3.5.0, [Game Input](../../docs/game_input) also supports VRMA.

There are some limitations:

<div class="doc-ul" markdown="1">

- Facial expression animations are not applied, even when VRMA file has them.
- In Expresion feature, `Hips` bone's position is constrained to keep its position. This might lead odd behavior if the VRMA is jump or crouch motion etc.
- The appearance may be odd for the motion with large yaw-angle change, like 360deg turn motion.

</div>


#### 2. How to prepare VRM Animation (.vrma) file
{: .doc-sec2 }

As of 2023 Dec, there are two ways to obtain VRM Animation file.

<div class="doc-ul" markdown="1">

- By [AnimationClipToVrmaSample](https://github.com/malaybaku/AnimationClipToVrmaSample) project, you can convert Humanoid AnimationClip data in Unity project to `.vrma` file. See detail in the repository (, though repository is JP based).
- UniVRM 0.114.0 and later version has feature to convert BVH file to `.vrma` files. Note that this convert process cannot maintain fingers' motion.


#### 3. Setup file
{: .doc-sec2 }

Before starting VMagicMirror, put VRMA file(`.vrma`) at `(My Document)\VMagicMirror_Files\Motions` folder.

If `Motions` folder does not exist, create new one.

The folder is same as referred in [Use Custom Motion](../use_custom_motion). VMagicMirror classify them based on the file extension.


#### 4. Setup for Game Input Feature
{: .doc-sec2 }

Start VMagicMirror and open settings about [Game Input](../../docs/game_input).

Then, specify how to run the motion for each input way.

<div class="doc-ul" markdown="1">

- Button, Mouse Click: Select motion to play from dropdown list.
- Keyboard: Assign key to each VRMA file.

</div>


#### 5. Use in Expressions Feature
{: .doc-sec2 }

Start VMagicMirror, and edit item accoring to [Expressions](../../docs/expressions) page.

In motion selection UI, select `Custom Motion` and choose the motion.

After the selection you can use the motion same way as built-in motion.

<div class="note-area" markdown="1">

**NOTE**

When the motion is not in the list or not played, then VRMA file has some unexpected content.

In this case,

- Confirm that VRMA file contains human body motion adta.
- Use UniVRM sample project to test whether the VRMA file works correctly.
- If the data is by third-party, please consider to contact them.。

</div>
