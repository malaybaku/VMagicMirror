---
layout: page
title: Download
---

# ダウンロード

VMagicMirrorはBOOTHからダウンロード可能です。

<a target="_blank" href="https://baku-dreameater.booth.pm/items/1272298/">
  <img class="full-width-mobile" src="https://asset.booth.pm/static-images/banner/468x60_02.png">
</a>

ダウンロードおよび利用にあたっては[License](../license)もご確認下さい。
ソースコードは[GitHub](https://github.com/malaybaku/VMagicMirror)で公開しています。

2種類のエディションを、4種類の方式で配布しています。

エディション: 

<div class="doc-ul" markdown="1">

- 基本エディション: 基本的なバージョンです。ほぼ全ての機能が使えます。
- フルエディション: 基本エディションにかかっているいくつかの機能制限が解除できるバージョンです。詳細は次のセクションをご覧下さい。

</div>

配布先:

<div class="doc-ul" markdown="1">

- [BOOTH無償版](https://baku-dreameater.booth.pm/items/1272298): 基本エディションです。もっとも基本的なVMagicMirrorの入手元です。
- [BOOTHブースト版](https://baku-dreameater.booth.pm/items/1272298): こちらも基本エディションで、ページ自体は無償版と同様です。純粋な応援目的でご使用下さい。
- [BOOTHフルエディション](https://baku-dreameater.booth.pm/items/3064040): フルエディションの買い切り版です。詳細は次のセクションをご覧下さい。
- [Fanbox](https://baku-dreameater.fanbox.cc/): アップデートのたび、フルエディションの最新版が入手できます。詳細は次節をご覧下さい。

</div>


### 基本エディションとフルエディションの差異について
{: .doc-sec1 }

VMagicMirrorでは基本エディションとフルエディションの2種類を配布しています。

基本エディションの場合、`ハンドトラッキングを有効化`をオンにしたとき、必ずアバターウィンドウに専用のエフェクトがかかります。
フルバージョンではこのエフェクトがオフにできます。

(左: 基本エディション / 右: フルエディション)

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking_edition_difference.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

また、v4.0.0以降では下記の制限もかかります。

<div class="doc-ul" markdown="1">

- VMC Protocolを有効化してデータを送信している場合、必ずアバターウィンドウに専用のエフェクトがかかります。ただし、データ送信中のデータの内容はフルエディションと共通です。
- VMC Protocolのデータ送信について、「ゲーム入力モード」でアバターの全身を動かしている間はデータが送信されません。
- サブキャラ機能で「インタラクションAPIを使用」をオンにした場合、必ずアバターウィンドウに専用のエフェクトがかかります。

</div>

以上の違いを除いて、基本エディションとフルエディションの機能は同様です。
ほぼ全ての機能は基本エディションでも使用できるため、エディションの差異がよく分からない場合はまずは基本エディションをご使用下さい。


フルエディションの価格は制限した機能のみに対するものではなく、VMagicMirrorが買い切りソフトだった場合のソフト全体のバリューを大まかに想定して設定していることに留意して下さい。

このほかに、フルエディションはドネーションウェア/シェアウェアとしての意味合いや、従来のブースト版以外での応援を受付の一環としての目的も兼ねたエディションになっています。


### フルエディションの入手方法
{: .doc-sec1 }

フルエディションの入手方法は2通りです。

<div class="doc-ul" markdown="1">

- [BOOTH](https://baku-dreameater.booth.pm/items/3064040)で買い切りバージョンを購入することで、今後の更新も含めてフルエディションを入手可能です。
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
- iPhone / iPad: Face ID対応、またはA14以降のチップ搭載であれば使用できます。詳しくは[外部トラッキング](../docs/external_tracker)のページをご覧下さい。
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

<a id="troubleshoot_first_startup"></a>

#### 初回の起動に失敗する場合のトラブルシューティング
{: .doc-sec2 }

初回の起動がうまくいかない場合、インストールに失敗している可能性があります。

サードパーティ製のアンチウイルスソフトを無効にして再度ダウンロード/インストールを試して下さい。

またzipファイル自体がダウンロード時に破損している可能性もあります。インストーラーと合わせてzipに同梱されているreadmeの内容を確認して下さい。

