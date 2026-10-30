---
layout: page
title: Load a VRM with Shaders Other Than MToon
lang: en
---

# Tips: Load a VRM with Shaders Other Than MToon

This page explains how to create a `.mouv` (Material Override Unity VRM) file with replacement materials and display it in VMagicMirror.

These steps are for users who already have a VRM model and non-MToon material assets they want to apply, for example after converting a VRChat avatar to VRM.

#### Features and Limitations
{: .doc-sec2 }

You can use a VRM as the base model and replace just its materials with materials of your choice. Parts without an override keep their original VRM materials.

<div class="doc-ul" markdown="1">

- Requires VMagicMirror v5.2.0 or later.
- The source model must be **VRM 1.0**. If you have a VRM 0.x model, re-export it as VRM 1.0 first.
- Replacement materials must use shaders that support **URP (Universal Render Pipeline)**. Correct rendering is not guaranteed for every shader or configuration.
- VRM expressions that use MaterialBinding, such as expressions that change material colors or UVs, are not guaranteed to work with material overrides.
- Export uses the original VRM data. Changes or additions to meshes, bones, components, and other model features in Unity are not included.
- `.mouv` is a different format from `.vrm`. It cannot be used directly in general VRM-compatible applications.

</div>

#### 1. Open the Template Project
{: .doc-sec2 }

Use the template project provided by [MaterialOverrideVrm](https://github.com/malaybaku/MaterialOverrideVrm). It includes the required packages and URP settings. Models and replacement materials are not included.

1. Download `MaterialOverrideVrm-Unity6-v0.2.0.zip` from the [v0.2.0 release page](https://github.com/malaybaku/MaterialOverrideVrm/releases/tag/v0.2.0) and extract it.
2. Install Unity 6000.3.13f1 (Unity 6.3 LTS) using Unity Hub.
3. In Unity Hub, add the extracted project folder containing `Assets`, `Packages`, and `ProjectSettings`, then open it with the Unity version above. Wait for package downloads and importing to finish.
4. Open `Assets/Scenes/Export.unity` from the Project window.

<div class="note-area" markdown="1">

**NOTE**

If you use Unity 2022, you can instead download `MaterialOverrideVrm-Unity2022-v0.2.0.zip` from the same release page and open it with Unity 2022.3.22f1.

</div>

<div class="note-area" markdown="1">

**NOTE: Installing into an Existing Project**

You can also install the package into an existing project through UPM. Back up the project before making changes.

Add the following UniVRM dependencies to `dependencies` in `Packages/manifest.json`:

```json
"com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4",
"com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4"
```

Then open `Window > Package Manager`, select `+ > Add package from git URL...`, and install the package using this URL:

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.2.0
```

Installing the package alone does not switch the project to URP. For Unity 6, also specify `"com.unity.render-pipelines.universal": "17.3.0"` in `dependencies` in `Packages/manifest.json` (use URP 14.0.10 for Unity 2022).

Open `Tools > Material Overridable VRM > URP Export Setup` and follow the dialog to apply the URP settings. Existing materials are not converted automatically, so prepare URP-compatible replacement materials as well.

</div>

#### 2. Prepare the VRM and Replacement Materials
{: .doc-sec2 }

1. Add the original VRM 1.0 file under `Assets` in the Project window and wait for importing to finish.
2. Drag the imported model prefab into the Hierarchy to place it in the scene, and set its position to the origin `(0, 0, 0)`.
3. Import your URP-compatible shaders, materials, and textures into the project. Set the shaders and parameters on your materials.
4. Select the model's root (its top-level GameObject) in the Hierarchy, then use `Add Component` in the Inspector to add `MaterialOverrideSource`.
5. Check that `Source Vrm` references the original VRM. If it was not assigned automatically, assign the original VRM from the Project window.
6. Expand `Bindings`. Material slots such as `Material 0` are grouped by Renderer names such as `Face` and `Body`. Check the original material in `Current`, then drag your replacement material into `Override` for each slot you want to replace.

`Current` is read-only. Slots with `Override` left as `None` use the original VRM material. You can assign the same material to multiple slots. Assign replacements through `Override`, rather than editing the Renderer's material fields directly.

Save the configured scene.

<div class="row">
{% include docimg.html file="/images/tips/mouv_export_inspector.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

Export may fail if a replacement material directly references a texture embedded in the original VRM. In that case, save the required texture as a separate asset, such as a PNG, and reference that asset from the material.

</div>

#### 3. Check the Preview
{: .doc-sec2 }

1. Press the Unity Editor's Play button to enter Play Mode.
2. Select the model's root in the Hierarchy and press `Preview Material Override (Play Mode)` on its `MaterialOverrideSource` component in the Inspector.
3. Check the Scene or Game view to confirm that the intended parts use the replacement materials and that colors, textures, and other visual settings look correct.
4. Press the Play button again to exit Play Mode.

<div class="row">
{% include docimg.html file="/images/tips/mouv_playmode_preview.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

Bindings cannot be changed while the preview is active. To make corrections, exit Play Mode, change the settings, and preview again. Changes made during Play Mode are lost when it ends, so adjust materials and Bindings in Edit Mode.

The preview restores the original materials when it ends. The `Override` assignments you made in Edit Mode are still used for export.

#### 4. Export the .mouv File
{: .doc-sec2 }

1. With Play Mode stopped, set the build target to Windows. Use `File > Build Profiles` in Unity 6 or `File > Build Settings` in Unity 2022 to check and switch the target.
2. Select the model's root and press `Export...` on `MaterialOverrideSource`.
3. Choose a destination and file name to export the `.mouv` file.

<div class="row">
{% include docimg.html file="/images/tips/mouv_export_button.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

#### 5. Load the File in VMagicMirror
{: .doc-sec2 }

In VMagicMirror v5.2.0 or later, the avatar file picker accepts `.mouv` as well as `.vrm`. Select the exported `.mouv` file to display the model with its replacement materials.
