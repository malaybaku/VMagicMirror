
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

# VMagicMirror

v0.8.3

* Author: Baxter
* 2019/Jun/03

The VRM avatar application without any special device.

1. Features
2. Download
3. Contact
4. (For Developers) Build


## 1. Features

* Load your VRM and show bust up 
* Show the keyboard and mouse operation in realtime.
* Change chromakey for your casting application.

The biggest feature is NO NEED for VR devices like HTC Vive, Oculus Rift, Leap motion.

It will be helpful in the following situations.

* Casting with almost no preparation
* Tech presentation with live coding

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

### 4.2. Asset install

* [UniVRM](https://dwango.github.io/vrm/) v0.51.0
* [VRoidSDK](https://vroid.pixiv.help/hc/ja/sections/360002815734-VRoid-SDK-SDK%E9%80%A3%E6%90%BA%E3%82%B5%E3%83%BC%E3%83%93%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6) v0.0.17
* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* [UniVRM](https://dwango.github.io/vrm/)
* [UniRx](https://github.com/neuecc/UniRx)
* [XinputGamepad](https://github.com/kaikikazu/XinputGamePad)
* [gRPC](https://github.com/grpc/grpc)
* [AniLipSync VRM](https://github.com/sh-akira/AniLipSync-VRM)
    + AniLipSync requires installation of [OVRLipSync v1.28.0](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/1.28.0/).
* [Unity Transform Control](https://github.com/mattatz/unity-transform-control)
* [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI)

*note: until v0.8.2a following libraries are required, but from v0.8.3 you can build the app without them.

* TextMesh Pro Essentials and Extra
* [VRoid SDK](https://vroid.pixiv.help/hc/ja/sections/360002815734-VRoid-SDK-SDK%E9%80%A3%E6%90%BA%E3%82%B5%E3%83%BC%E3%83%93%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6) v0.0.17
* [Unity Transform Control](https://github.com/mattatz/unity-transform-control)


Should be noted that `FinalIK` and `Dlib FaceLandmark Detector` are paid asset, and you need to submit the application to get VRoid SDK.

Dlib FaceLandmark Detector requires dataset file to be moved into `StreamingAssets` folder. Please check the file is in correct location by running Dlib FaceLandmark Detector example scenes like `WebCamTexture Example`.

gRPC install is available from daily builds. 
Please see the detail at [gRPC instruction](https://github.com/grpc/grpc/tree/master/src/csharp/experimental). Current version uses `v1.12.0-dev`, but might work with newer build.

### 4.3. Build

* Unity: Specify `Bin` folder for the output.
* WPF build creates exe on `Bin` folder in `ConfigApp` (create if it does not exist).

note: zip file distributed in BOOTH consists of the files of `Bin`, without some unnecessary files.
