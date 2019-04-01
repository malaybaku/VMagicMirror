
[Japanese Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README.md)

# VMagicMirror

* Author: Baxter
* 2019/Mar/17

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

* [UniVRM](https://dwango.github.io/vrm/)
* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [UniVRM](https://dwango.github.io/vrm/)
* [UniRx](https://github.com/neuecc/UniRx)
* [XinputGamepad](https://github.com/kaikikazu/XinputGamePad)
* Text Mesh Pro

Should be noted that FinalIK is paid asset, and you need to submit the application to get VRoid SDK.

### 4.3. Apply Noto font

VMagicMirror uses Noto font for the TextMeshPro and Japanese font asset is required to show the texts. You need the asset but it is very large so the file is not included in the repository.

Please output the font asset file from `Assets/Fonts/NotoSansJP-Regular.otf`.

You will see the way to create asset by google with two words, "TextMeshPro" and "Font Asset Creator".

### 4.4. Build

* Unity: Specify `Bin` folder for the output.
* WPF build creates exe on `Bin` folder in `ConfigApp` (create if it does not exist).

note: zip file distributed in BOOTH consists of the files of `Bin`, without some unnecessary files.
