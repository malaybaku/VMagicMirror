
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

# VMagicMirror

v0.9.5

* Author: Baxter
* 2019/Dec/14

The VRM avatar application without any special device.

1. Features
2. Download
3. Contact
4. (For Developers) Build
5. (For Developers) Create MOD

## 1. Features

* Load your VRM and show bust up 
* Show the keyboard and mouse operation in realtime.
* Change chromakey for your casting application.

The biggest feature is NO NEED for VR devices like HTC Vive, Oculus Rift, Leap motion.

It will be helpful in the following situations.

* Casting with almost no preparation
* Tech presentation with live coding
* Desktop mascot

## 2. Download

[Booth](https://booth.pm/en/items/1272298).

Run on Windows 10.

Please see [Manual](https://github.com/malaybaku/VMagicMirror/blob/master/doc/manual_en.md) for the detail.

## 3. Contact

* [Twitter](https://twitter.com/baku_dreameater)
* [Blog](https://www.baku-dreameater.net/)

note: Contact in English or Japanese is very helpful for the author.

## 4. (Developer) How to build

### 4.1. Folder structure

Set the folder as following.

+ `Bin`
    + (Empty directory)
+ `Unity`
    + This repository
+ `WPF`
    + [WPF repository](https://github.com/malaybaku/VMAgicMirrorConfig)

Open Unity project with Unity 2018.3.x, and open WPF project with Visual Studio 2019.

Maintainer's environment is as following.

* Unity 2018.4.13f1 Personal
* Visual Studio Community 2019
    * .NET Core 3.1 SDK
    * Visual Studio Component "C++ Desktop Development" is required in install.

### 4.2. Asset install

* [UniVRM](https://dwango.github.io/vrm/) v0.53.0
* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* [UniVRM](https://dwango.github.io/vrm/)
* [UniRx](https://github.com/neuecc/UniRx)
* [XinputGamepad](https://github.com/kaikikazu/XinputGamePad)
* [AniLipSync VRM](https://github.com/sh-akira/AniLipSync-VRM)
    + AniLipSync requires installation of [OVRLipSync v1.28.0](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/1.28.0/).
* [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI)
* [Zenject](https://github.com/svermeulen/Extenject)
* [Deform](https://github.com/keenanwoodall/Deform)
* DOTween (from Asset Store)

`Deform` is downloaded by package manager style.

Should be noted that `FinalIK` and `Dlib FaceLandmark Detector` are paid asset, and you need to submit the application to get VRoid SDK.

Dlib FaceLandmark Detector requires dataset file to be moved into `StreamingAssets` folder. Please check the file is in correct location by running Dlib FaceLandmark Detector example scenes like `WebCamTexture Example`.

### 4.3. Build

* Unity: Specify `Bin` folder for the output.
* To build WPF project, right click `VMagicMirrorConfig` project on the solution explorer and select `publish`.
    - Use following profile setting as `Folder Profile`.
        - Configuration: `Debug | x86`
        - Target Framework: `netcoreapp3.0`
        - Deployment Mode: `Self Contained`
        - Target Runtime: `win10-x86`
        - Target Location: choose somewhere on your PC folder
    - By the publish you will get the files at target location folder. Then, copy the files to `Bin/ConfigApp/` folder.

Distributed VMagicMirror (v0.9.3 or later) also would be a reference of the folder structure.

## 5. About Model Data from SketchFab

The model data `xbox_controller.fbx` included in this repository is from SketchFab, with Attribution 4.0 International (CC BY 4.0).

Creator: Criegrrunov
URL: https://sketchfab.com/3d-models/xbox-controller-fb71f28a6eab4a2785cf68ff87c4c1fc

In VMagicMirror, the materials are replaced for the visual consistency.


## 6. (For Developers) Create MOD

VMagicMirror v0.9.3 or later supports MOD library (dll) loading system. In this way you can add your new feature without editing VMagicMirror itself.

Please see the detail at [VMagicMirrorModExample](https://github.com/malaybaku/VMagicMirrorModExample) repository.

