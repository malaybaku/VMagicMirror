---
layout: page
title: VMC Protocol
---

# VMC Protocol

この機能はVMagicMirror v3.3.0から追加されました。

[VMC Protocol](https://protocol.vmc.info/)に対応した他アプリケーションからのデータを受信してアバターに適用できます。

<div class="row">
{% include docimg.html file="/images/docs/vmcp_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### 注意: VMC Protocolを使う前に
{: .doc-sec2 }

VMC ProtocolはVMagicMirrorがサポートする機能の中では発展的な機能であり、かつ安定性を保証しにくい機能です。

VMagicMirrorの開発者は下記ソフトウェアを接続先として動作確認をしていますが、
下記ソフトウェアを含めてアプリケーションの更新に伴った不具合等のリスクを理解のうえでご使用下さい。

<div class="doc-ul" markdown="1">

- [LuppetX 1.0.5](https://luppet.jp/)
- [WebcamMotionCapture 1.9.0](https://webcammotioncapture.info/)

</div>

とくに、異なる端末間でのVMC Protocolの送受信はアプリケーション次第で高負荷の原因になることに注意して下さい。



#### 1. 基本的な使い方
{: .doc-sec2 }

本機能は初期状態では完全に無効になっています。

`詳細設定ウィンドウを開く`から詳細設定を開き、`VMCP`タブから`設定タブをメインウィンドウに表示`を選択します。

この操作によってコントロールパネルの`VMCP`タブが表示され、本機能が利用可能になります。

<div class="row">
{% include docimg.html file="/images/docs/vmcp_enable.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/vmcp_settings.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>


これ以降はコントロールパネル側のタブで設定を行います。

`VMCPを有効にする`をオンにすると受信処理が有効になります。

続けて、データの受信元となるアプリケーションの設定を確認して下記を指定したのち、「変更を適用」で適用すると受信状態が更新されます。

<div class="doc-ul" markdown="1">

- ポート番号
- 頭の姿勢、手の姿勢、表情のいずれを適用するか
- アプリ名: これは単なるメモ用のエリアです。実際の挙動には影響しません。

</div>

<div class="note-area" markdown="1">

**NOTE**

VMC Protocolの受信で手の姿勢を適用すると、VMagicMirrorがデフォルトで行っているモーション(キーボードのタイピング動作等)は発生しなくなります。

ただし、VMC Protocolの受信中であっても[Word to Motion](./expressions)による表情切り替え等は優先的に適用されます。

</div>

受信に成功しているあいだ、`接続`エリアにチェックマークが表示されます。

本機能を安定して使うための推奨事項として、送信側ソフトではVMagicMirrorで使っているのと同一のVRMをロードして下さい。

<div class="note-area" markdown="1">

**NOTE**

VMagicMirrorでは送信側ソフトで異なるアバターを使っている可能性を想定して姿勢データを処理をしています。
見た目に問題が感じられない場合、アバターが不一致のまま本機能を使っても差し支えありません。

</div>


#### 2. 詳細設定
{: .doc-sec2 }

詳細設定では以下の項目を指定できます。多くの場合、これらの項目はデフォルト値のままで問題ありません。

<div class="doc-ul" markdown="1">

- `補正なしで送信元ボーンの姿勢を適用`: オンにすると、受信した姿勢に特に補正を適用せずに適用します。とくに手の姿勢を適用するとき、肩やひじの挙動に違和感があればオンにして下さい。
- `VMCPの受信中はカメラ機能をオフ`: オンにすると、VMC Protocolを使用中はカメラ画像の取得を行わなくなります。デフォルトでオンになっています。

</div>


#### 3. 既知の問題と対処方法
{: .doc-sec2 }

VMagicMirror v3.3.1時点で以下の問題を確認しています。

<div class="doc-ul" markdown="1">

- `補正なしで送信元ボーンの姿勢を適用`をオンにしている場合、Word to Motion機能のうなづきモーション/拍手モーションが動かない事があります。

</div>


