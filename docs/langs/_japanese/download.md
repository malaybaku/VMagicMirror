---
layout: page
title: Download
permalink: /download
---

# ダウンロード

VMagicMirrorはBOOTHからダウンロード可能です。

<a target="_blank" href="https://baku-dreameater.booth.pm/items/1272298/">
  <img src="https://asset.booth.pm/static-images/banner/468x60_02.png">
</a>

ダウンロードおよび利用にあたっては[License](./license)もご確認下さい。
ソースコードは[GitHub](https://github.com/malaybaku/VMagicMirror)で公開しています。

v1.8.0以降では2種類のエディションを、4種類の方式で配布しています。

エディション: 

<div class="doc-ul" markdown="1">

- 基本エディション: 基本的なバージョンです。ほぼ全ての機能が使えます。
- フルエディション: ハンドトラッキング中の機能制限がないバージョンです。詳細は次節をご覧下さい。

</div>

配布先:

<div class="doc-ul" markdown="1">

- [BOOTH無償版](https://baku-dreameater.booth.pm/items/1272298): 基本エディションです。もっとも基本的なVMagicMirrorの入手元です。
- [BOOTHブースト版](https://baku-dreameater.booth.pm/items/1272298): こちらも基本エディションで、ページ自体は無償版と同様です。純粋な応援目的でご使用下さい。
- [BOOTHフルエディション](https://baku-dreameater.booth.pm/items/3064040): フルエディションの買い切り版です。詳細は次節をご覧下さい。
- [Fanbox](https://baku-dreameater.fanbox.cc/): アップデートのたび、フルエディションの最新版が入手できます。詳細は次節をご覧下さい。

</div>


### 基本エディションとフルエディションの差異について
{: .doc-sec1 }

v1.8.0以降のVMagicMirrorは基本エディションとフルエディションの2種類を配布しています。

基本エディションの場合、`ハンドトラッキングを有効化`をオンにしたとき、必ずキャラクターウィンドウに専用のエフェクトがかかります。
フルバージョンではこのエフェクトがオフにできます。

(左: 基本エディション / 右: フルエディション)

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking_edition_difference.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

上記の違いを除き、無償エディションとフルエディションの機能は全く同じです。

フルエディションの価格はハンドトラッキング単体ではなく、VMagicMirrorが有償頒布ソフトだった場合のソフト全体のバリューを想定した価格設定としてご理解下さい。

このほか、ドネーションウェア/シェアウェアとしての意味合いや、従来のBOOST版以外での応援受付の一環としての目的も兼ねています。


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

<a id="troubleshoot_first_startup"></a>

#### 初回の起動に失敗する場合のトラブルシューティング
{: .doc-sec2 }

初回の起動がうまくいかない場合、インストールに失敗している可能性があります。

サードパーティ製のアンチウイルスソフトを無効にして再度ダウンロード/インストールを試して下さい。

またzipファイル自体がダウンロード時に破損している可能性もあります。インストーラーと合わせてzipに同梱されているreadmeの内容を確認して下さい。

v2.0.0以降のバージョンについて、本ページでzipファイルのサイズとMD5ハッシュを掲載しています。

<div class="doc-ul" markdown="1">

- zipファイルのサイズは、Windowsエクスプローラ上でzipファイルを右クリックし、プロパティを開くと確認できます。
- MD5ハッシュを確認するには、zipファイルのあるフォルダ上でコマンドプロンプトを開き、以下のようなコマンドを入力します。

</div>

コマンドの例: (ファイル名はバージョンに応じて読み替えて下さい)

```
certutil -hashfile VMM_v2.0.2_Standard_Installer.zip MD5
```

|--------------------------+-------------------------+----------------------|
| Version                  | Zip File Size (byte)    | MD5 Hash             |
|:------------------------:|:-----------:|:---------------------------------|
| v2.0.0 Standard Edition  | 126,983,144 | 6610c9b81aa493f02917f68daa275b7d |
| v2.0.0 Full Edition      | 127,062,779 | b00a734f2548ad0c66025300a0986b6b |
| v2.0.1 Standard Edition  | 127,187,092 | dc468640e4eb11302a8ca6bcfc83db3e |
| v2.0.1 Full Edition      | 127,195,414 | 8d6ecb6e4d5bb90585f96bd5144b4a5e |
| v2.0.2 Standard Edition  | 127,206,743 | 13976a1d60b585bec32bf3c02f90d4ac |
| v2.0.2 Full Edition      | 127,065,845 | e5d210852116840bc567c16a22d6b014 |
| v2.0.3 Standard Edition  | 127,206,042 | df9052ef8dd0debccb61d12833943360 |
| v2.0.3 Full Edition      | 127,038,447 | aff91773799f03a97a0ecf538afbf43e |
|==========================|=============|==================================|
