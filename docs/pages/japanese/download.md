---
layout: page
title: Download
permalink: /download
---

[English](./en/download)

# ダウンロード

VMagicMirrorは[BOOTH](https://booth.pm/ja/items/1272298)からダウンロード可能です。

<a target="_blank" href="https://baku-dreameater.booth.pm/items/1272298/">
  <img src="https://asset.booth.pm/static-images/banner/468x60_02.png">
</a>

ダウンロードおよび利用にあたっては[License](./license)もご確認下さい。
ソースコードは[GitHub](https://github.com/malaybaku/VMagicMirror)で公開しています。

v1.8.0以降では2種類のエディションを、計4種類の方式で配布しています。

エディション: 

<div class="doc-ul" markdown="1">

- 基本エディション: 基本的なバージョンです。ほぼ全ての機能が使えます。
- フルエディション: ハンドトラッキング中の機能制限がないバージョンです。詳細は次節をご覧下さい。

</div>

配布方式:

<div class="doc-ul" markdown="1">

- BOOTH無償版: 基本エディションです。もっとも基本的なVMagicMirrorの入手元です。
- BOOTHブースト版: こちらも基本エディションです。純粋な応援目的でご使用下さい。
- BOOTHフルエディション: フルエディションの買い切り版です。詳細は次節をご覧下さい。
- Fanbox: アップデートのたび、フルエディションの最新版が入手できます。詳細は次節をご覧下さい。

</div>


### 基本エディションとフルエディションの差異について
{: .doc-sec1 }

v1.8.0以降のVMagicMirrorは基本エディションとフルエディションの2種類を配布しています。

基本エディションの場合、`ハンドトラッキングを有効化`をオンにしたとき、必ずキャラクターウィンドウに専用のエフェクトがかかります。
いっぽうフルバージョンの場合、ハンドトラッキング中のエフェクトを無効にできます。

(TODO: エフェクトがかかった状態のスクリーンショット)

上記の差異を除いて、無償エディションとフルエディションの機能は全く同じです。

フルエディションの価格はハンドトラッキング単体ではなく、VMagicMirrorが有償頒布ソフトだった場合のソフト全体のバリューを想定した価格設定としてご理解下さい。
これに加えてシェアウェアとしての意味合いや、(BOOST版以外での)応援受付の一環としての目的も兼ねています。


### フルエディションの入手方法
{: .doc-sec1 }

フルエディションの入手方法は2通りです。

<div class="doc-ul" markdown="1">

- [BOOTH](https://booth.pm/ja/items/1272298)で買い切りバージョンを購入することで、今後の更新も含めてフルエディションを入手可能です。
- [Fanbox](https://baku-dreameater.fanbox.cc/)で有償プラン(300円以上)に加入することで、BOOTHにリリースされる最新バージョンをその都度入手可能です。

</div>

Fanboxは退会した場合、それ以降のアップデートが入手できなくなります。
またFanboxとBOOTHでは同時にアップデートが公開され、先行公開がないことにも注意して下さい。



### 必要なPC環境
{: .doc-sec1 }

#### 必須の環境
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Windows 10 64bit

</div>

#### オプションで対応しているデバイスなど
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- マイク: 通常のマイクのほか、仮想マイク入力(ボイスチェンジャー出力など)も使用できます。
- Webカメラ: 視野角が狭めで、顔全体が映るものを使って下さい。VMagicMirrorでは原則としてカメラ画像を320x240ピクセルに圧縮して用いるため、解像度が低いWebカメラでも問題ありません。
- ゲームパッド: XInputに対応したゲームパッドか、またはDUAL SHOCK 4に対応しています。Xbox One Controllerで動作を確認しています。
- iPhone / iPad: Face ID対応、またはA14以降のチップ搭載であれば使用できます。詳しくは[外部トラッキング](./docs/external_tracker)のページをご覧下さい。
- MIDIコントローラ: ほぼなんでも対応していますが、キー入力のみ対応しているため、キー中心のコントローラを推奨しています。

</div>

#### 動作しないことがある環境
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- CPU: AMDの古いCPUをお使いの環境ではアプリが起動しないこともあります。
- GPU: グラフィック環境が古い場合、モデルの顔部分のテクスチャが表示されないことがあります。

</div>

#### 動作チェック環境
{: .doc-sec2 }

開発者は以下の環境で動作チェックをしています。もし手元のPCでVMagicMirrorの動作が不自然に重い場合、PC環境を記載のうえ開発者までご連絡下さい。

**環境1: デスクトップPC**

<div class="doc-ul" markdown="1">

- CPU: Intel Core i7-6700K
- GPU: GeForce GTX 1080
- ウェブカメラ: C922 Pro Stream Webcam
- マイク入力:
    - VoiceMeeter Bananaの出力
    - VT-4 WET (変声済みのVT-4の出力)
    - C922 Pro Stream Webcam

</div>

**環境2: ノートPC(Surface Book 2)**

<div class="doc-ul" markdown="1">

- ウェブカメラ: PC本体フロントエンドカメラ
- マイク入力: PC本体マイク

</div>
