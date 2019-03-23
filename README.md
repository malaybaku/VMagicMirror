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

### 4.2. アセットの導入

* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)
* [VRoid SDK](https://vroid.pixiv.help/hc/ja/sections/360002815734-VRoid-SDK-SDK%E9%80%A3%E6%90%BA%E3%82%B5%E3%83%BC%E3%83%93%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6)
* [UniVRM](https://dwango.github.io/vrm/)
* [UniRx](https://github.com/neuecc/UniRx)
* [XinputGamepad](https://github.com/kaikikazu/XinputGamePad)
* Text Mesh Pro

FinalIKが有償アセットであること、およびVRoid SDKは2019年3月時点でSDKの取得に個別の申請が必須であることに注意してください。

### 4.3. Notoフォントの適用

developブランチ以降のブランチでビルドするには
`Assets/Fonts/NotoSansJP-Regular.otf`をもとにText Mesh Pro用のフォントを生成します。

やり方は[UnityのText Mesh Proアセットで日本語を使うときの手順](https://qiita.com/thorikawa/items/03b65b75fa9461b53efd)などを参考にしてください。

### 4.4. ビルド

* Unityでのビルド時には`Bin`フォルダを指定します。
* WPFでのビルドでは、ビルド設定で`Bin`以下の`ConfigApp`フォルダに実行ファイルが出力されます。

※BOOTHで配布されているzipの内容は、`Bin`フォルダ以下から不要なファイルを除いたものです。
