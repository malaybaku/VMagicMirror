---
layout: page
title: Use MToon Material in 3D Accessory
lang: en
---

# Tips: Use MToon Material in GLB Accessory

The page explains how to apply MToon shader based material to `.glb` data which is available for [Accessory](../../docs/accessory) feature.

This feature is available in VMagicMirror v4.2.1 and later version.

<div class="note-area" markdown="1">

**NOTE**

At the time of v4.2.1 release, there are few `.glb` data which contains MToon material data.

You need to setup and export model data by following steps.

</div>


#### 1. Create Unity Project to Export .glb File
{: .doc-sec2 }

Install Unity 2022.3 or later version and create a new project. Choose Built-in Render Pipeline for the project.

<div class="note-area" markdown="1">

**NOTE**

VMagicMirror uses Built-in Render Pipeline.

Please select Built-in Render Pipeline so that your object to export looks similarly in Unity Editor and VMagicMirror.

</div>

In the new project, introduce following packages.

- [UniVRM v0.121.0](https://github.com/vrm-c/UniVRM/releases/tag/v0.121.0)
- [UnityMToonGltfExtension v0.1.0](https://github.com/malaybaku/UnityMToonGltfExtension/releases/tag/v0.1.0)

To introduce packages, open `Window > Package Manager` in Unity Editor.

Select `+` button at the top left of the window, select `Add Package from git URL...`, then input following packages' URL to install them.

<div class="doc-ul" markdown="1">

- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRMShaders#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/UniGLTF#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM10#v0.121.0`
- `https://github.com/malaybaku/UnityMToonGltfExtension.git?path=/Package#v0.1.0`

</div>

#### 2. Export .glb File
{: .doc-sec2 }

Create or import 3D asset prefab into the project, and attach material with MToon shader and setup parameters. Note that you can use several formats (`.fbx` or other data) in this step.

Open export window from `MToonGltf -> Export MToon glTF...`. Select the prefab and export the data as `.glb`.

You might see some warnings about material fallback in export window, but you can ignore them.

If you want to check export is successful, you can test it by importing exported `.glb` file.

Enable `MToonGltf -> Use MToon glTF Importer` on the menu bar, and then drag & drop exported `.glb` into the project. If your setup is correct, the imported model has material with MToon shader.


#### 3. Use the File in VMagicMirror
{: .doc-sec2 }

The exported files are available with same way as conventional accessory data.

Put the `.glb` files in `(My Document)\VMagicMirror_Files\Accessory` folder and start VMagicMirror so that the app recognizes added accessories.
