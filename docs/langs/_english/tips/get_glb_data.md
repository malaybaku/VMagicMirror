---
layout: page
title: How to Get GLB Data
permalink: /en/tips/get_glb_data
lang_prefix: /en/
---

[Japanese](../../tips/get_glb_data)


# Tips: How to Get GLB Data

This page is about how to get GLB or GLTF 3D model data available for [Accessory Feature](../docs/accessory).

As GLB/GLTF is open source data specification, so there are many tools supporting them.

There are three major approaches, and this page goes in detail for first two ways.

<div class="doc-ul" markdown="1">

1. Export exising 3D model as GLB on Unity Editor
2. Download from [SketchFab](https://sketchfab.com)
3. Export GLB file from DCC tools like Blender

</div>

For DCC tool, please google how to export the model as glb.

<div class="note-area" markdown="1">

**NOTE**

Please check license of the models carefully, just like other type of creatives (avatars and images).

Also you should be very conscious about that, exporting process might lead degrade the model's appearance.

</div>


#### Export exising 3D model as GLB on Unity Editor
{: .doc-sec2 }

Create a new project, or open an existing project, to import [UniVRM](https://github.com/vrm-c/UniVRM). See other web pages to check how to install it.

Put the model object to export, on scene's world origin.

If you want to edit model's position or rotation, or model's scale is not equal to `1`, then create a new GameObject and put it to world origin. Set model object to the child of that new GameObject, and then you can edit position, rotation and scale of the model.

After the setup, choose model or model's parent object and execute `UniGLTF > Export (glb)` to save the GLB file.

<div class="row">
{% include docimg.html file="./images/tips/accessory_glb_export_unity.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

You can check whether the export is successful, by drag & drop the file into the Unity project again and load the generated prefab.

The appearnce might be odd if the model uses material with non-standard shader. In this case, consider to setup the material with standard shader and retry to export.

BTW, the example uses [Anime Rooms](https://assetstore.unity.com/packages/3d/props/interior/anime-rooms-75722) asset.

</div>


#### Download from SketchFab
{: .doc-sec2 }

[SketchFab](https://sketchfab.com) is the web page of 3D models, and some of them are available for a fee or for free.

When downloading the model, you can choose `gltf` data format.

<div class="row">
{% include docimg.html file="./images/tips/accessory_gltf_from_sketchfab.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

In Sketchfab you can see the license on downloading.

Also, if the model seems to have some appearance issue, then please consider to download model with Original Format. Then you will be able to import it to Unity and setup materials, and then export to GLB, to get right appearance.

</div>


