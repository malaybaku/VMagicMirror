---
layout: page
title: Use Custom Motion
permalink: /en/tips/use_custom_motion
---

[English](../tips/use_custom_motion)

# Tips: Use Custom Motion in VMagicMirror

This feature is available in v1.6.0 and later.

Following steps enable to use non-built-in motions in VMagicMirror.

1. On Unity Editor, export motion files available on VMagicMirror.
2. Put exported motion file to specific folder.
3. Select the motion on Word to Motion feature.

#### Prerequisite
{: .doc-sec2 }

- You need to export motion in Unity Editor.
- You do NOT need the knowledge about Unity C# script.
- It is good to know about Animation Clip, but 

#### Available Formats and Limitations
{: .doc-sec2 }

Format:

- You can export motion which is recognized as Humanoid Animation, in Unity 2019.4.x.
    - Pure AnimationClip created in Unity 2019.4 is available.
    - Motions in fbx file for Humanoid model will also be available, though there are exceptions.
    
Limitations:

- Root pose and lower body motion is not played.
    - This is because VMagicMirror originally assumes upper body only motion.
- IK based hand motion is not played, though exported in the data.
    - Hand IK will be supported in future updates.
- Cannot play loop animation.

#### 1. Export Motion
{: .doc-sec2 }

In Unity 2019.4.14f1, create a new project or open existing one.

Then visit [VMagicMirror_MotioExporterのReleases](https://github.com/malaybaku/VMagicMirror_MotionExporter/releases) to get latest version `.unitypackage`.

Import the `.unitypackage`, and open scene `Assets/Baku/VMagicMirror_MotionExporter/Scenes/MotionExporter`.

In the scene, select `Exporter` object and see `Motion Exporter` component. Set the `AnimationClip` you want to export to `Export Target`.

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_export_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Then press `Export` button to save the file in `StreamingAssets` folder. The file addition might be not reflected, and in this case please check file in file explorer, or restart Unity.

The file name is like `(AnimationClip name).vmm_motion`. As long as you maintain the file extension you can change the file name.

<div class="note-area" markdown="1">

**Tips**

At this step you can check the file export is actually successful, by following step.

1. Import [UniVRM](https://github.com/vrm-c/UniVRM).
2. Import a VRM model and put on `MotionExporter` scene.
3. Select `MotionTestPlayer` and setup `MotionTestPlay` component.
    - `FileName`: Motion file name in `StreamingAssets` folder.
    - `Target`: VRM model
    - `OnlyUpperBody`: On (*on by default)
4. Play the scene.

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_verify_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

</div>

#### 2. Put the Exported Motion File
{: .doc-sec2 }

Exported motion file (`.vmm_motion`) to `(VMagicMirror.exe folder)/Motions` folder.

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_placement.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>


#### 3. Word to Motion Setup
{: .doc-sec2 }

Start VMagicMirror and open Word to Motion item edit window. If this step is unclear, see [Expression](../docs/expressions) page.

Choose `Custom Motion` in motion option, then select motion.

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

After the setup, the motion will be available almost same as built-in motion.

<div class="note-area" markdown="1">

NOTE:

If you do not see the motion selecion, then exported data format might incorrect.

In this case see `1. Export Motion` and try the check process written in tips.

</div>
