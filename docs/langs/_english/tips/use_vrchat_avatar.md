---
layout: page
title: Use a VRChat Avatar in VMagicMirror
lang: en
---

# Tips: Use a VRChat Avatar in VMagicMirror
{: .no_toc }

This guide explains how to convert an avatar you have set up for VRChat and use it in VMagicMirror.

{% include docimg.html file="/images/tips/use_vrchat_avatar_example.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}

Avatar shown: [Shinano](https://booth.pm/en/items/6106863) by @ぽんでろ

<div class="note-area" markdown="1">

- VMagicMirror v5.2.0 or later is required. We recommend v5.3.0 or later.
- The features described here work the same way in the free and paid editions.
- This guide assumes you are familiar with Unity, the VRChat SDK, and VCC or ALCOM.

</div>

<div class="toc-area" markdown="1">

#### Table of Contents
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### Avatar formats you can load
{: .doc-sec2 }

To use a VRChat avatar in VMagicMirror, export it in one of these formats.

<div class="doc-ul" markdown="1">

- **VRM (`.vrm`)** stores the avatar's mesh, bones, expressions, and spring bones in a single file.
- **Material Override Unity VRM (`.mouv`)** combines VRM data with materials. VMagicMirror applies the materials stored in the file to the VRM model, so the avatar can use shaders other than MToon.

</div>

The `.mouv` format was designed to support custom shaders. As of November 2026, it is effectively specific to VMagicMirror.

<div class="note-area" markdown="1">

**Note on redistributing `.mouv` files**

A `.mouv` file includes compiled shaders as well as the avatar model and textures. Before sharing the file with someone else or making it available for download, check that the terms for each asset allow redistribution in this format. This includes any clothing or other assets you have added.

</div>

#### Choose how to prepare your avatar
{: .doc-sec2 }

If you already have a `.vrm` file and are happy with how it looks, load it directly in VMagicMirror.

If you want to keep your existing VRM and replace only its materials, follow [Load a VRM with Shaders Other Than MToon](../use_material_override_vrm) to create a `.mouv` file from the VRM and your replacement materials. Then skip ahead to [Load the avatar in VMagicMirror](#load-the-avatar-in-vmagicmirror).

If you do not have a VRM yet, or want to export the avatar again from your VRChat Unity project, use [Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC) to export a `.mouv` file as described below.

#### Limitations of the `.mouv` format
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- **Shaders** must support the Universal Render Pipeline (URP). Shaders that work only with the Built-in Render Pipeline cannot be used.
- **Spring bones and constraints** carry over to the extent that VRM can represent them.
- **Expressions** generally need to be defined in the VRM. Perfect Sync can also use mesh blendshapes directly, as described below.
- **Animations and gimmicks** that rely on animation do not carry over. A `.mouv` file contains no animation data.
- **Expressions that change materials**, such as those that change material colors or UVs, may not work as intended after you replace the materials.

</div>

VMagicMirror uses URP, while VRChat's asset system targets the Built-in Render Pipeline as of November 2026. Check that the shaders your avatar uses also work with URP.

The other limitations follow from the way the file stores the avatar. Apart from the materials, the avatar data comes from the VRM.

To import motion separately, VMagicMirror supports VRM Animation. See [Use VRM Animation in VMagicMirror](../use_vrma).

#### Export the avatar
{: .doc-sec2 }

[Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC) creates a `.mouv` file from your VRChat Unity project. The package converts the avatar to VRM using NDMF VRM Exporter and packages it with the materials the avatar used before conversion.

<div class="doc-ul" markdown="1">

1. Add [baxter's VPM Packages](https://malaybaku.github.io/baku-vpm/) to VCC or ALCOM and install `Material Override VRM for VRChat`.
2. Follow the package instructions to install the dependencies and configure the avatar for VRM export.
3. Export the avatar and save the `.mouv` file. The recommended option to export everything in one pass (`まとめてエクスポート`) also saves a `.vrm` file.

</div>

For detailed steps, see the [package usage guide](https://github.com/malaybaku/MaterialOverrideVrmVRC/blob/main/README_usage.md). For help setting up expressions and spring bones for VRM export, see the [NDMF VRM Exporter documentation](https://github.com/hkrn/ndmf-vrm-exporter).

#### Load the avatar in VMagicMirror
{: .doc-sec2 }

On the `Home` tab, click `Load from File` and select your `.mouv` file, or drag the file onto the tab. Both methods work just as they do for a `.vrm` file.

<div class="note-area" markdown="1">

**Note**

Depending on the avatar, a `.mouv` file can be larger and contain more shader and material data than a VRM, so it often takes longer to load.

</div>

If the avatar looks wrong, compare it with the exported `.vrm` to help identify what needs adjusting. You can check the export settings against the [package usage guide](https://github.com/malaybaku/MaterialOverrideVrmVRC/blob/main/README_usage.md).

Once loaded, a `.mouv` avatar works like a VRM avatar apart from its materials. The usual [VMagicMirror documentation](../../docs) applies from here.

#### Set up Perfect Sync
{: .doc-sec2 }

Perfect Sync lets your avatar follow subtle facial movements using a webcam or a Face ID–enabled iPhone or iPad. See [Perfect Sync](../perfect_sync) for expression setup and [External Tracking](../../docs/external_tracker) for instructions on connecting an iPhone or iPad app.

If you are adding ARKit blendshapes to a VRChat avatar, there are two ways to make them available to Perfect Sync. Either method can use a subset of the 52 shapes. If you can choose the blendshape names, the first method is simpler.

<div class="doc-ul" markdown="1">

1. Add the blendshapes directly to the model using the names listed in [Perfect Sync](../perfect_sync).
2. Add the blendshapes to the model, then define the corresponding VRM expressions under `Custom Expression` in `VRM Export Description` before exporting. Use the [Perfect Sync expression names](../perfect_sync) for the shapes you want to track.

</div>

For example, the [Vtuber ARKit52 FacialTracking BlendShapeKey AddOn](https://charlotta.booth.pm/items/6961828) adds 52 ARKit blendshapes to supported avatar models. The product page lists compatible avatars, setup instructions, and terms of use.

If you use the first method, check these two settings:

- [Avatar Optimizer](https://github.com/anatawa12/AvatarOptimizer) may remove Perfect Sync blendshapes during VRM export. To keep them, make sure a component recognized by the VRChat SDK references those blendshapes.
- By default, VMagicMirror ignores blendshapes without a corresponding VRM expression. In the advanced settings, open `Face` > `Perfect Sync` and enable `Use Perfect Sync blendshapes in meshes when expressions are missing`.
