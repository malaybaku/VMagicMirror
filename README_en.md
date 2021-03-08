
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

![Logo](./docs/images/vmagicmirror_logo.png)

Logo: by [@otama_jacksy](https://twitter.com/otama_jacksy)

v1.6.1

* Author: Baxter
* 2021/Jan/31

The VRM avatar application without any special device.

1. Features
2. Download
3. Contact
4. (For Developers) Build
5. Third-Party License
6. About Localization Contribution

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

Please see [Manual](https://malaybaku.github.io/VMagicMirror/) for the detail.

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

Open Unity project with Unity 2019.4.x, and open WPF project with Visual Studio 2019.

Maintainer's environment is as following.

* Unity 2019.4.1f1 Personal
* Visual Studio Community 2019.16.6.3
    * .NET Core 3.1 SDK
    * Visual Studio Component "C++ Desktop Development" is required.

### 4.2. Asset install

* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088)
* [UniVRM](https://dwango.github.io/vrm/) v0.61.1
* [UniRx](https://github.com/neuecc/UniRx) (from Asset Store)
* [OVRLipSync v1.28.0](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/1.28.0/)
* [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI/releases) v0.3
* [Zenject](https://github.com/svermeulen/Extenject) (from Asset Store)
* [MidiJack](https://github.com/malaybaku/MidiJack)
* SharpDX.DirectInput 4.2.0
    * [SharpDX](https://www.nuget.org/packages/SharpDX)
    * [SharpDX.DirectInput](https://www.nuget.org/packages/SharpDX.DirectInput/)
* [RawInput.Sharp](https://www.nuget.org/packages/RawInput.Sharp/) 0.0.3
* DOTween (from Asset Store)
* [Fly,Baby. ver1.2](https://nanakorobi-hi.booth.pm/items/1629266)
* [LaserLightShader](https://noriben.booth.pm/items/2141514)
* [VMagicMirror_MotionExporter](https://github.com/malaybaku/VMagicMirror_MotionExporter)

Should be noted that `FinalIK`, `Dlib FaceLandmark Detector`, and `OpenCV for Unity` are paid assets.

"Fly,Baby." and "LaserLightShader" are available on BOOTH, and they are optional. If you do not introduce them, some of typing effects will not work correctly.

Also should be careful that `MidiJack` used in VMagicMirror is forked one.

Dlib FaceLandmark Detector requires dataset file to be moved into `StreamingAssets` folder. Please check the file is in correct location by running Dlib FaceLandmark Detector example scenes like `WebCamTexture Example`.

Install SharpDX by following steps.

- From 2 URLs get `.nupkg` file by `Download package`, and expand them as zip file.
- In the expanded zip, see `lib/netstandard1.3/` to get file `SharpDX.dll` and `SharpDX.DirectInput.dll`. Put these file in anywhere on the Unity project.

RawInput.Sharp can be installed with almost same work flow.

- Get `.nupkg` from NuGet gallery and expand as zip to get `lib/netstandard1.1/RawInput.Sharp.dll`
- Create `RawInputSharp` folder in Unity project's Assets folder, and put dll into the folder.

For the UniVRM, you need to modify 2 points.

In `Assets/VRM/UniHumanoid/Scripts/HumanPoseTransfer.cs` and add a line,

```
//...
        HumanPoseHandler m_handler;
    
        //Add following line
        public HumanPoseHandler PoseHandler => m_handler;
    
        public void OnEnable()
        {
//...
```

Also, in `Assets/VRM/UniVRM/Scripts/BlendShape/VRMBlendShapeProxy.cs`,

```
//...
        //Add this method
        public void ReloadBlendShape()
        {
            m_merger?.RestoreMaterialInitialValues(BlendShapeAvatar.Clips);
            if (BlendShapeAvatar != null)
            {
                m_merger = new BlendShapeMerger(BlendShapeAvatar.Clips, transform);                
            }
        }
//...
```

### 4.3. Build

* In Unity,
    - Specify `Bin` folder for the output.
* To build WPF project, right click `VMagicMirrorConfig` project on the solution explorer and select `publish`.
    - Use following profile setting as `Folder Profile`.
        - Configuration: `Release | x86`
        - Target Framework: `netcoreapp3.0`
        - Deployment Mode: `Self Contained`
        - Target Runtime: `win10-x86`
        - Target Location: choose somewhere on your PC folder
    - By the publish you will get the files at target location folder. Then, copy the files to `Bin/ConfigApp/` folder.

Distributed VMagicMirror (v0.9.3 or later) also would be a reference of the folder structure.

## 5. Third-Party License

### 5.1. OSS License

OSS license is listed in control panel GUI.

[https://github.com/malaybaku/VMagicMirrorConfig](https://github.com/malaybaku/VMagicMirrorConfig)

You can also see the plain text version from below.

https://github.com/malaybaku/VMagicMirrorConfig/blob/master/VMagicMirrorConfig/VMagicMirrorConfig/Resources/LicenseTextResource.xaml


### 5.2. About Model Data from SketchFab

The model data `xbox_controller.fbx` included in this repository is from SketchFab, with Attribution 4.0 International (CC BY 4.0).

Creator: Criegrrunov
URL: https://sketchfab.com/3d-models/xbox-controller-fb71f28a6eab4a2785cf68ff87c4c1fc

In VMagicMirror, the materials are replaced for the visual consistency.


## 6. About Localization Contribution

Please check [about_localization.md](./about_localization.md), when you plan to contribute by localization activity.
