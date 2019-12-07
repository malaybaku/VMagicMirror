[English Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README_en.md)

# VMagicMirror

v0.9.4

* 作成: 獏星(ばくすたー)
* 2019/12/07

WindowsでVRMを表示し、追加のデバイスなしで動かせるアプリケーションです。

1. できること
2. ダウンロード
3. 質問など
4. (開発者向け)ビルド手順
5. (開発者向け)MODを作成する手順

## 1. できること

* VRMを読み込み、キャラクターの上半身を表示します。
* キーボードとマウス操作をモーションとして反映します。
* 可変のクロマキーが適用できます。

キーボードとマウス操作のみでキャラクターが動く特徴から、以下のシチュエーションで活躍します。

* 機材の準備が面倒な時の配信
* ライブコーディング中の賑やかし
* デスクトップマスコット

## 2. ダウンロード

[Booth](https://booth.pm/ja/items/1272298)から取得可能です。

Windows 10環境でお使いいただけます。

操作方法については[マニュアル](https://github.com/malaybaku/VMagicMirror/blob/master/doc/manual.md)をご覧下さい。

## 3. 質問など

* [Twitter](https://twitter.com/baku_dreameater)
* [Blog](https://www.baku-dreameater.net/)


## 4. (開発者向け)ビルド手順

### 4.1. フォルダ配置

適当なフォルダ以下に、次の構成で配置します。

+ `Bin`
    + (空のディレクトリ)
+ `Unity`
    + このレポジトリ
+ `WPF`
    + [WPFのレポジトリ](https://github.com/malaybaku/VMAgicMirrorConfig)

Unity 2018.3系でUnityプロジェクトを開き、Visual Studio 2019でWPFプロジェクトを開きます。

メンテナの開発環境は以下の通りです。

* Unity 2018.4.13f1 Personal
* Visual Studio Community 2019 16.3.7
    * .NET Core 3.0 SDKがインストール済みであること

### 4.2. アセットの導入

* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* [UniVRM](https://github.com/vrm-c/UniVRM) v0.53.0
* [UniRx](https://github.com/neuecc/UniRx)
* [XinputGamepad](https://github.com/kaikikazu/XinputGamePad)
* [AniLipSync VRM](https://github.com/sh-akira/AniLipSync-VRM)
    + AniLipSyncが依存している[OVRLipSync v1.28.0](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/1.28.0/)のインストールも必要です。
* [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI)
* [Zenject](https://github.com/svermeulen/Extenject)

FinalIKおよびDlib FaceLandmark Detectorが有償アセットであることに注意してください。

Dlib FaceLandmark Detectorについては、アセットに含まれるデータセットを`StreamingAssets`フォルダ以下に移動します。導入にあたっては、Dlib FaceLandmark Detector本体のサンプルプロジェクト(`WebCamTextureExample`)を動かすなどして、ファイルが正しく置けているか確認します。


### 4.3. ビルド

* Unityでのビルド時には`Bin`フォルダを指定します。
* WPFでのビルドでは、`VMagicMirrorConfig`プロジェクトを右クリックし、`発行`を指定してフォルダ上にアプリケーションを配置します。
    - プロファイル設定は次のようにします。
        - 構成: `Debug | x86`
        - ターゲットフレームワーク: `netcoreapp3.0`
        - 配置モード: `自己完結`
        - ターゲットランタイム: `win10-x86`
        - ターゲットの場所: PC上の適当なフォルダ
    - 上記の設定で発行すると、ターゲットのフォルダ上に`VMagicMirror.exe`を含むファイル群が出力されます。れらのファイルを`Bin/ConfigApp/`以下にコピーします。

フォルダ構成については配布されているVMagicMirrorも参考にしてください。

## 5. MODを作成する手順

VMagicMirror v0.9.3以降ではライブラリ(dll)形式のMOD読み込みがサポートされているため、VMagicMirror自体を編集する代わりにMODで機能を追加することもできます。

詳細は[VMagicMirrorModExample](https://github.com/malaybaku/VMagicMirrorModExample)を参照下さい。

