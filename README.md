[English Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README_en.md)

![Logo](./docs/images/vmagicmirror_logo.png)

Logo: by [@otama_jacksy](https://twitter.com/otama_jacksy)

v4.3.0

* 作成: 獏星(ばくすたー)
* 2025/10/11

WindowsでVRMを表示し、追加のデバイスなしで動かせるアプリケーションです。

1. できること
2. ダウンロード
3. 質問など
4. (開発者向け)ビルド手順
5. OSS等のライセンス
6. ローカリゼーションについて


## 1. できること

* VRMを読み込み、アバターの上半身を表示します。
* キーボードとマウス操作をモーションとして反映します。
* 可変のクロマキーが適用できます。

キーボードとマウス操作のみでアバターが動く特徴から、以下のシチュエーションで活躍します。

* 機材の準備が面倒な時の配信
* ライブコーディング中の賑やかし
* デスクトップマスコット

## 2. ダウンロード

[Booth](https://booth.pm/ja/items/1272298)から取得可能です。

Windows 10/11環境でお使いいただけます。

操作方法については[マニュアル](https://malaybaku.github.io/VMagicMirror/)をご覧下さい。

## 3. 質問など

* [Twitter](https://twitter.com/baku_dreameater)


## 4. (開発者向け)ビルド手順

### 4.1. レポジトリの配置

適当なフォルダに本レポジトリを配置します。配置先について、空白文字を含むようなフォルダパスは避けて下さい。

Unity 6.0系でUnityプロジェクト(本レポジトリの`VMagicMirror`フォルダ)を開き、Visual Studio 2022でWPFプロジェクトを開きます。

メンテナの開発環境は以下の通りです。

* Unity 6.0.58f2 Personal
* Visual Studio Community 2022 (17.14.11)
    * 「.NET Desktop」コンポーネントがインストール済みであること
    * 「C++によるデスクトップ開発」コンポーネントがインストール済みであること
        - UnityのBurstコンパイラ向けに必要なセットアップです。


### 4.2. アセットの導入

* Unity Asset Storeから:
    * DOTween
    * [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
    * [Dlib FaceLandmark Detector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314)
* Asset Store以外から:
    * [Oculus LipSync Unity Integration v29](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/)
    * [VRMLoaderUI](https://github.com/m2wasabi/VRMLoaderUI/releases) v0.3
    * SharpDX.DirectInput 4.2.0
        * [SharpDX](https://www.nuget.org/packages/SharpDX)
        * [SharpDX.DirectInput](https://www.nuget.org/packages/SharpDX.DirectInput/)
    * [RawInput.Sharp](https://www.nuget.org/packages/RawInput.Sharp/) 0.0.3
    * [Fly,Baby. ver1.2](https://nanakorobi-hi.booth.pm/items/1629266)
    * [LaserLightShader](https://noriben.booth.pm/items/2141514)
    * [MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin), [v1.16.1](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.1) or later
    * Roslyn Scripting (後述)

FinalIK, Dlib FaceLandmark Detectorは有償アセットであることに注意してください。

"Fly,Baby." および "LaserLightShader"はBOOTHで販売されているアセットで、ビルドに必須ではありませんが、もし導入しない場合、タイピング演出が一部動かなくなります。

Dlib FaceLandmark Detectorについては、アセットに含まれるデータセットを`StreamingAssets`フォルダ以下に移動します。導入にあたっては、Dlib FaceLandmark Detector本体のサンプルプロジェクト(`WebCamTextureExample`)を動かすなどして、ファイルが正しく置けているか確認します。

MediaPipeUnityPluginについては、モデルデータ(`.bytes`　ファイル)のうち下記のファイルを `StreamingAssets/MediaPipeTracker` フォルダ以下に移動します。

- `face_landmarker_v2_with_blendshapes.bytes`
- `hand_landmarker.bytes`
- `holistic_landmarker.bytes`

SharpDXは次の手順で導入します。

- 2つのNuGetギャラリーの`Download package`から`.nupkg`ファイルを取得し、それぞれ`.zip`ファイルとして展開します。
- 展開したzip内の`lib/netstandard1.3/`フォルダにそれぞれ`SharpDX.dll`および`SharpDX.DirectInput.dll`があるので、これらをUnityプロジェクト上の適当な場所に追加します。

RawInput.Sharpもほぼ同様の導入手順です。

- NuGetギャラリーから取得した`.nupkg`を展開し、中の`lib/netstandard1.1/RawInput.Sharp.dll`を取得します。
- 取得したDLLを、Unityプロジェクト上でAssets以下に`RawInputSharp`というフォルダを作り、その下に追加します。

Roslyn Scriptingについては、NuGet Packageの下記を取得し、必要なdllをプロジェクト上に配置します。

- `Microsoft.CodeAnalysis.CSharp.Scripting-v4.8.0`
- `System.Runtime.Loader-v4.0.0`

メンテナのUnityプロジェクト環境では、Assets以下に下記のようなフォルダ構造でdllを配置しています。

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

なお、NuGetForUnityでもRoslyn Scriptingに関するパッケージを導入できる可能性がありますが、本readmeの記載時点では動作は確認していません。

以上のインストールで `Assets` 直下に追加されたフォルダについては、`Assets/Ignored` フォルダを作成し、この `Ignored` フォルダ内に移動することを推奨しています(ソース管理の対象から外れます)。

そのほか、手作業での導入は不要ですが、Unity Package Managerで下記を参照しています。

* [Zenject](https://github.com/svermeulen/Extenject) v9.3.1
* [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
* [UniVRM](https://github.com/vrm-c/UniVRM) v0.121.0
* [R3](https://github.com/Cysharp/R3)
* [UniTask](https://github.com/Cysharp/UniTask)
* [KlakSpout](https://github.com/keijiro/KlakSpout)
* [MidiJack](https://github.com/malaybaku/MidiJack)
    * オリジナルのMidiJackではなく、Forkレポジトリです。
* [uWindowCapture](https://github.com/hecomi/uWindowCapture) v1.1.2
* [uOSC](https://github.com/hecomi/uOSC) v2.2.0

NuGetForUnityからは下記を参照しています。ライブラリはPackagesフォルダ内に格納されます。

* [R3](https://github.com/Cysharp/R3)
* [NAudio](https://github.com/naudio/NAudio)


### 4.3. ビルド

### 4.3.1. コマンドラインからビルドする

`Batches`フォルダ内のコマンドからビルドが可能です。
バッチファイル等の引数の指定方法については、ファイル内のコメントを参照して下さい。

Unityについては諸々のアセットを導入済みであることが必要なこと、およびビルドに用いるUnityバージョンが`build_unity.cmd`で指定されていることに注意して下さい。
事情があって異なるバージョンのUnityエディタをビルドに用いる場合、`build_unity.cmd`内のUnityのパスを修正します。

また、`create_installer.cmd`を使用するには[Inno Setup](https://jrsoftware.org/isinfo.php)のインストールが必要です。

```
# WPFプロジェクトをビルド
build_wpf.cmd standard dev

# Unityプロジェクトをビルド
build_unity.cmd standard dev

# WPF/Unityプロジェクトをビルド後に呼ぶことでインストーラを作成
create_installer.cmd standard dev v1.2.3

# version.txtに書いてあるバージョン値を用いて、ビルドおよびインストーラの作成までを実行
job_release_instraller.cmd
```

### 4.3.2. プロジェクトを開いてビルドする

`Bin`など、出力先フォルダを準備します。以下はフォルダ名が`Bin`である想定での説明ですが、他のフォルダ名でも構いません。

* Unityでのビルド時には`Bin`フォルダを指定します。
* WPFでのビルドでは、`VMagicMirrorConfig`プロジェクトを右クリックし、`発行`を指定してフォルダ上にアプリケーションを配置します。
    - プロファイル設定は次のようにします。
        - 構成: `Release | x86`
        - ターゲットフレームワーク: `netcoreapp3.0`
        - 配置モード: `自己完結`
        - ターゲットランタイム: `win10-x86`
        - ターゲットの場所: PC上の適当なフォルダ
    - 上記の設定で発行すると、ターゲットのフォルダ上に`VMagicMirror.exe`を含むファイル群が出力されます。これらのファイルを`Bin/ConfigApp/`以下にコピーします。

フォルダ構成を確認したい場合、実際に配布されているVMagicMirrorも参考にしてください。

## 5. その他のレポジトリに含まれないアセット

VMagicMirror v4.0.0で導入されたサブキャラ機能に関連して、 `BuddyPresetResources.asset` に設定されたプリセット扱いのアセットデータ( `.bytes` )は公開したレポジトリに含まれないため、上記のビルド手順を追ってもデータが適切に読み込めません。

これはプリセットのサブキャラが第三者に制作依頼したものであり、ライセンスも異なるためです。

必要に応じて、`BuddyPresetResources.asset` の `Texture Binary` と `VRM Binary` に下記のような適当なダミーアセットを適用してください。

- `Texture Binary` :256x256px程度の適当なpngファイルの拡張子を `.bytes` に変更したもの
- `VRM Binary` :軽量な適当なVRMファイルの拡張子を `.bytes` に変更したもの

ref: (docページにサブキャラの取り扱いを追記次第、そのURLへのリンクを追記します)

## 6. OSS等のライセンス

### 6.1. OSSライセンス

GUI中でOSSライセンスを掲載しており、その文面は下記ファイルで管理しています。

https://github.com/malaybaku/VMagicMirror/blob/master/WPF/VMagicMirrorConfig/VMagicMirrorConfig/Resources/LicenseTextResource.xaml

過去に使用したものも含むライセンス情報は以下に記載しています。

https://malaybaku.github.io/VMagicMirror/credit_license

また、本レポジトリに含む画像の一部は [Otomanopee](https://github.com/Gutenberg-Labo/Otomanopee) フォントを使って作成しています。フォント自体を再配布するものではないため、あくまで補足情報として記載しています。


### 6.2. Creative Commons Licenseに基づくモデルについて

このレポジトリに含まれる下記モデルはCreative Commons Attributionライセンスに基づいて使用し、レポジトリに含まれます。

- `Gamepad.fbx` (作成者: Negyek)
- `CarSteering.glb` (作成者: CaskeThis)

VMagicMirrorでは元モデルに対し、他のデバイスとの一貫性を保つためにマテリアルを適用しているほか、カスタマイズのためにテクスチャを変更可能にしています。

## 7. ローカリゼーションについて

日本語、英語以外のローカリゼーションでのContributionに興味がある場合、[about_localization.md](./about_localization.md)を参照して下さい。

