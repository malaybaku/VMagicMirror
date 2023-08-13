---
layout: page
title: VMC Protocol
---

# VMC Protocol

VMagicMirrorはv4.0.0で`VMCP`タブを追加しました。

このタブの設定により[VMC Protocol](https://protocol.vmc.info/)に対応した他アプリケーションからのデータを受信してアバターに適用できます。

(TODO: この画像をVMCPタブの画像に変更)

<div class="row">
{% include docimg.html file="/images/docs/devices_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### 注意: VMC Protocolを使う前に
{: .doc-sec2 }

VMC ProtocolはVMagicMirrorがサポートする機能の中では発展的な機能であり、かつ安定性を保証しにくい機能です。

VMagicMirrorの開発者は下記ソフトウェアを接続先として動作確認をしていますが、
下記ソフトウェアを含めてアプリケーションの更新に伴った不具合等のリスクを理解のうえでご使用下さい。

<div class="doc-ul" markdown="1">

- [LuppetX 1.0.5](https://luppet.jp/)
- [WebcamMotionCapture 1.9.0](https://webcammotioncapture.info/)
- (TODO: スマホも何かはほしい、安定性に注意して選びたい)

</div>

とくに、端末間でのVMC Protocolの送受信はアプリケーション次第で高負荷の原因になることに注意して下さい。



#### 1. 基本的な使い方
{: .doc-sec2 }

本機能を使うには`VMCPを有効にする`をオンにします。

続けて、データの受信元となるアプリケーションの設定を確認して下記を指定したのち、「変更を適用」で適用すると受信を開始、または停止します。

- ポート番号
- 頭、手、表情のいずれを適用するか

<div class="note-area" markdown="1">

**NOTE**

VMC Protocolの受信で手の姿勢を適用すると、VMagicMirrorがデフォルトで行っているモーション(キーボードのタイピング動作等)は発生しなくなります。

ただし、VMC Protocolの受信中であっても[Word to Motion](./expressions)による表情切り替え/モーションは優先的に適用されます。

</div>


本機能を安定して使うために、送信側ソフトではVMagicMirrorで使っているのと同一のVRMをロードすることを推奨しています。

<div class="note-area" markdown="1">

**NOTE**

VMagicMirrorでは送信側ソフトで異なるアバターを使っている可能性を想定して姿勢データを処理をしています。
見た目に問題が感じられない場合、アバターが異なる状態のまま本機能を使っても差し支えありません。

</div>


#### 2. 詳細設定
{: .doc-sec2 }

詳細設定では以下の項目を指定できます。多くの場合、これらの項目はデフォルト値のままで問題ありません。

<div class="doc-ul" markdown="1">

- `VMCPの受信中はカメラ機能をオフ`: オンにすると、VMC Protocolを使用中はカメラ画像の取得を行わなくなります。デフォルトでオンになっています。

</div>
