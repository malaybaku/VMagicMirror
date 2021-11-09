
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

![Logo](./docs/images/vmagicmirror_logo.png)

Logo: by [@otama_jacksy](https://twitter.com/otama_jacksy)

v1.9.1

* Author: Baxter
* 2021/Oct/24

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

Put the repository on your local folder. folder path should not include space character.

Open Unity project with Unity 2020.3.x, and open WPF project with Visual Studio 2022.

Maintainer's environment is as following.

* Unity 2020.3.8f1 Personal
* Visual Studio Community 2022 17.0.0
    * Component ".NET Desktop Development" is required.
    * Also Component "C++ Desktop Development" is required, for Unity Burst compiler.

### 4.2. Asset install

* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088)
* [OVRLipSync v1.28.0](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/1.28.0/)
* [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI/releases) v0.3
* [Zenject](https://github.com/svermeulen/Extenject) (from Asset Store)
* SharpDX.DirectInput 4.2.0
    * [SharpDX](https://www.nuget.org/packages/SharpDX)
    * [SharpDX.DirectInput](https://www.nuget.org/packages/SharpDX.DirectInput/)
* [RawInput.Sharp](https://www.nuget.org/packages/RawInput.Sharp/) 0.0.3
* [uWindowCapture](https://github.com/hecomi/uWindowCapture) v1.0.2
* DOTween (from Asset Store)
* [Fly,Baby. ver1.2](https://nanakorobi-hi.booth.pm/items/1629266)
* [LaserLightShader](https://noriben.booth.pm/items/2141514)
* [VMagicMirror_MotionExporter](https://github.com/malaybaku/VMagicMirror_MotionExporter)
* [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)

Should be noted that `FinalIK`, `Dlib FaceLandmark Detector`, and `OpenCV for Unity` are paid assets.

[NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) is necessary to import [NAudio](https://github.com/naudio/NAudio).

"Fly,Baby." and "LaserLightShader" are available on BOOTH, and they are optional. If you do not introduce them, some of typing effects will not work correctly.

Dlib FaceLandmark Detector requires dataset file to be moved into `StreamingAssets` folder. Please check the file is in correct location by running Dlib FaceLandmark Detector example scenes like `WebCamTexture Example`.

Install SharpDX by following steps.

- From 2 URLs get `.nupkg` file by `Download package`, and expand them as zip file.
- In the expanded zip, see `lib/netstandard1.3/` to get file `SharpDX.dll` and `SharpDX.DirectInput.dll`. Put these file in anywhere on the Unity project.

RawInput.Sharp can be installed with almost same work flow.

- Get `.nupkg` from NuGet gallery and expand as zip to get `lib/netstandard1.1/RawInput.Sharp.dll`
- Create `RawInputSharp` folder in Unity project's Assets folder, and put dll into the folder.

For OpenCVforUnity, edit `DisposableOpenCVObject.cs`: 

```
    abstract public class DisposableOpenCVObject : DisposableObject
    {

//        internal IntPtr nativeObj;
        //Change to public member
        public IntPtr nativeObj;

```

Also there are some UPM based dependencies.

* [UniVRM](https://github.com/vrm-c/UniVRM) v0.66.0
* [UniRx](https://github.com/neuecc/UniRx)
* [MidiJack](https://github.com/malaybaku/MidiJack)
    * This is fork repository and not the original.
    
### 4.3. Build

Prepare output folder like `Bin`. Following instruction expects the folder name is `Bin`, but of course you can specify other name.

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

When you want to check right folder structure, please see the distributed app.

## 5. Third-Party License

### 5.1. OSS License

OSS license is listed in control panel GUI, and the resource text is this file.

https://github.com/malaybaku/VMagicMirror/blob/master/WPF/VMagicMirrorConfig/VMagicMirrorConfig/Resources/LicenseTextResource.xaml

This page is similar, but it also refers to the libraries which are used in past versions.

https://malaybaku.github.io/VMagicMirror/credit_license


### 5.2. About Gamepad Model Data 

`Gamepad.fbx` in this repository is introduced in #616 , and the model is under Attribution 4.0 International (CC BY 4.0).

Author: Negyek

VMagicMirror applies material for visual consistency, and allow texture replacement to support visual customize.

## 6. About Localization Contribution

Please check [about_localization.md](./about_localization.md), when you plan to contribute by localization activity.
