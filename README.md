[English Readme](https://github.com/malaybaku/VMagicMirror/blob/master/README_en.md)

# VMagicMirror

* 作成: 獏星(ばくすたー)
* 2019/03/17

WindowsでVRMを表示し、追加のデバイスなしで動かせるアプリケーションです。

1. できること
2. ダウンロード
3. 質問など
4. (開発者向け)ビルド手順

## 1. できること

* VRMを読み込み、キャラクターの上半身を表示します。
* キーボードとマウス操作をモーションとして反映します。
* 可変のクロマキーが適用できます。

キーボードとマウス操作のみでキャラクターが動く特徴から、以下のシチュエーションで活躍します。

* 機材の準備が面倒な時の配信
* ライブコーディング中の賑やかし

## 2. ダウンロード

[Booth](https://baku-dreameater.booth.pm/)に公開しています。

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

### 4.2. アセットの導入

* [UniVRM](https://dwango.github.io/vrm/)
* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)

### 4.3. ビルド

* Unityでのビルド時には`Bin`フォルダを指定します。
* WPFでのビルドでは、ビルド設定で`Bin`以下の`ConfigApp`フォルダに実行ファイルが出力されます。

※BOOTHで配布されているzipの内容は、`Bin`フォルダ以下から不要なファイルを除いたものです。


