
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

(**NOTE**: not published yet)

[Booth](https://baku-dreameater.booth.pm/).

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

### 4.3. Build

* Unity: Specify `Bin` folder for the output.
* WPF build creates exe on `Bin` folder in `ConfigApp` (create if it does not exist).

note: zip file distributed in BOOTH consists of the files of `Bin`, without some unnecessary files.
