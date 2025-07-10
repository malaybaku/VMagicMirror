
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

![Logo](./docs/images/vmagicmirror_logo.png)

Logo: by [@otama_jacksy](https://twitter.com/otama_jacksy)

v4.1.0

* Author: Baxter
* 2025/Jul/13

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

Run on Windows 10 and Windows 11.

Please see [Manual](https://malaybaku.github.io/VMagicMirror/) for the detail.

## 3. Contact

* [Twitter](https://twitter.com/baku_dreameater)

note: Contact in English or Japanese is very helpful for the author.

## 4. (Developer) How to build

### 4.1. Folder structure

Put the repository on your local folder. folder path should not include space character.

Open Unity project with Unity 6.0.x, and open WPF project with Visual Studio 2022.

Maintainer's environment is as following.

* Unity 6.0.33f1 Personal
* Visual Studio Community 2022 (17.13.0)
    * Component ".NET Desktop Development" is required.
    * Also Component "C++ Desktop Development" is required, for Unity Burst compiler.

### 4.2. Asset install

* From Asset Store:
    * DOTween
    * [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
    * [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* Other
    * [Oculus LipSync Unity Integration v29](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/)
    * [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI/releases) v0.3
    * SharpDX.DirectInput 4.2.0
        * [SharpDX](https://www.nuget.org/packages/SharpDX)
        * [SharpDX.DirectInput](https://www.nuget.org/packages/SharpDX.DirectInput/)
    * [RawInput.Sharp](https://www.nuget.org/packages/RawInput.Sharp/) 0.0.3
    * [Fly,Baby. ver1.2](https://nanakorobi-hi.booth.pm/items/1629266)
    * [LaserLightShader](https://noriben.booth.pm/items/2141514)
    * [MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin), [v1.16.1](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.1) or later
    * Roslyn Scripting (see the last part of this section for detail)

Note that `FinalIK` and `Dlib FaceLandmark Detector` are paid assets.

"Fly,Baby." and "LaserLightShader" are available on BOOTH, and they are optional. If you do not introduce them, some of typing effects will not work correctly.

Dlib FaceLandmark Detector requires dataset file to be moved into `StreamingAssets` folder. Please check the file is in correct location by running Dlib FaceLandmark Detector example scenes like `WebCamTexture Example`.

About MediaPipeUnityPlugin, move following model files (`.bytes`) in the package into `StreamingAssets/MediaPipeTracker` folder.

- `face_landmarker_v2_with_blendshapes.bytes`
- `hand_landmarker.bytes`


Install SharpDX by following steps.

- From 2 URLs get `.nupkg` file by `Download package`, and expand them as zip file.
- In the expanded zip, see `lib/netstandard1.3/` to get file `SharpDX.dll` and `SharpDX.DirectInput.dll`. Put these file in anywhere on the Unity project.

RawInput.Sharp can be installed with almost same work flow.

- Get `.nupkg` from NuGet gallery and expand as zip to get `lib/netstandard1.1/RawInput.Sharp.dll`
- Create `RawInputSharp` folder in Unity project's Assets folder, and put dll into the folder.

To install Roslyn Scripting library, get following packages from NuGet to introduce .dll files.

- `Microsoft.CodeAnalysis.CSharp.Scripting-v4.8.0`
- `System.Runtime.Loader-v4.0.0`

In maintainers' project, the folder and file structure is as following.

- `Assets/CSharpScripting`
    - `Microsoft.CodeAnalysis.CSharp.Scripting-v4.8.0/Plugins`
        - Microsoft.CodeAnalysis.CSharp.Scripting.dll
        - Microsoft.CodeAnalysis.dll
        - Microsoft.CodeAnalysis.Scripting.dll
        - System.Buffers.dll
        - System.Collections.Immutable.dll
        - System.Memory.dll
        - System.Numerics.Vectors.dll
        - System.Reflection.Metadata.dll
        - System.Runtime.CompilerServices.Unsafe.dll
        - System.Text.Encoding.CodePages.dll
        - System.Threading.Tasks.Extensions.dll
        - Microsoft.CodeAnalysis.CSharp.dll
    - `System.Runtime.Loader-v4.0.0/Plugins`
        - System.Runtime.Loader.dll

*NuGetForUnity might get the packages related to Roslyn Scripting correctly, though it is not tested yet.

I recommend to create `Assets/Ignored` folder and move `Assets/*` folders introduced by above steps. You should do this if you want to ignore those 3rd party assets in version control.

Also there are some UPM based dependencies.

* [Zenject](https://github.com/svermeulen/Extenject) v9.3.1
* [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
* [UniVRM](https://github.com/vrm-c/UniVRM) v0.121.0
* [UniRx](https://github.com/neuecc/UniRx)
* [KlakSpout](https://github.com/keijiro/KlakSpout)
* [MidiJack](https://github.com/malaybaku/MidiJack)
    * This is fork repository and not the original.
* [uWindowCapture](https://github.com/hecomi/uWindowCapture) v1.1.2
* [uOSC](https://github.com/hecomi/uOSC) v2.2.0

Following packages are installed via NuGetForUnity.

* [NAudio](https://github.com/naudio/NAudio)


### 4.3. Build

### 4.3.1. Build by Cmd Files

Build operation is available by `.cmd` files in `Batches` folder.

Please see what args are supported in each files.

For Unity build, you have to prepare required assets beforehand, and note that Unity Editor version is strictly specified in `build_unity.cmd`.
If you have some reason to use different version of editor, you need to modify `build_unity.cmd`.

Note that, `create_installer.cmd` requires [Inno Setup](https://jrsoftware.org/isinfo.php) to be installed.

```
# Build WPF project
build_wpf.cmd standard dev

# Build Unity project
build_unity.cmd standard dev

# Call after building WPF and Unity project to make installer
create_installer.cmd standard dev v1.2.3

# Build and create installer, with version name written in "version.txt"
job_release_instraller.cmd
```

### 4.3.2. Build on Opened Project

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


## 5. Note about Missing Assets

VMagicMirror v4.0.0 and later version supports Buddy feature, and `BuddyPresetResources.asset` will have missing binary data (`.bytes`). 

This is because the preset buddy assets were created by a third party on a commissioned basis.

When necessary, please assign dummy assets at `Texture Binary` and `VRM Binary` field of `BuddyPresetResources.asset`.

- `Texture Binary` : 256x256px image png file, with extension changed to `.bytes`
- `VRM Binary` : Light VRM model, with extension to `.bytes`

ref: (Add URL reference to doc web page, when preset buddy's license note is added)


## 6. Third-Party License

### 6.1. OSS License

OSS license is listed in control panel GUI, and the resource text is this file.

https://github.com/malaybaku/VMagicMirror/blob/master/WPF/VMagicMirrorConfig/VMagicMirrorConfig/Resources/LicenseTextResource.xaml

This page is similar, but it also refers to the libraries which are used in past versions.

https://malaybaku.github.io/VMagicMirror/credit_license

Note that some images are created with [Otomanopee](https://github.com/Gutenberg-Labo/Otomanopee) font. This is not license notice, since the font itself is not redistributed.

### 6.2. About Model data under Creative Commons Attribution

This repository includes following model data files under Attribution 4.0 International (CC BY 4.0).

- `Gamepad.fbx` (Author: Negyek)
- `CarSteering.glb` (Author: CaskeThis)

VMagicMirror applies material for visual consistency, and allow texture replacement to support visual customize.


## 7. About Localization Contribution

Please check [about_localization.md](./about_localization.md), when you plan to contribute by localization activity.
