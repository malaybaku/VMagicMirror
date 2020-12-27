---
layout: page
title: Docs
permalink: /docs
---

[English](./en/docs)

# 詳細設定

ここでは[Get Started](./get_started)で紹介したものより高度な機能を2つ紹介します。


#### 外部トラッキング (External Tracker)
{: .doc-sec2 }

VMagicMirror v1.1.0以降では他アプリと連携したモーション、および表情トラッキングが利用できます。

v1.1.0の時点ではiOSアプリの`iFacialMocap`をサポートしています。

詳細は[外部トラッキング](./docs/external_tracker)を参照ください。


#### 設定ウィンドウ
{: .doc-sec2 }

コントロールパネルの`ホーム`タブで`設定ウィンドウを開く`ボタンを押すと設定ウィンドウが開きます。

{% include docimg.html file="/images/docs/setting_window.png" %}

設定ウィンドウは6つのカテゴリーで分類されています。詳細は各ページにてご覧下さい。

|----------------------------------+------------------------------------------------------|
| カテゴリ名                       | できること                                           |
|:--------------------------------:|:-----------------------------------------------------|
| [ウィンドウ](./docs/window)      | キャラクター表示ウィンドウの制御                     |
| [モーション](./docs/motion)      | キャラクターの体型や動き方にあわせた調整             |
| [レイアウト](./docs/layout)      | カメラ、キーボード、タッチパッド、ゲームパッドの配置 |
| [エフェクト](./docs/effects)     | ライト、影、Bloom、風の調整                          |
| [デバイス](./docs/devices)       | ゲームパッド、MIDIコントローラの接続設定             |
| [表情の制御](./docs/expressions) | 表情やモーションを切り替える機能の設定               |
|==================================|======================================================|


<div class="note-area" markdown="1">

**NOTE**

v1.5.0およびそれ以前のバージョンでは`デバイス`タブが存在せず、かわりに`レイアウト`タブ内で`デバイス`タブ相当の機能がサポートされています。

</div>

#### 設定をリセットするには
{: .doc-sec2 }

設定ウィンドウでは多くの設定をカテゴリ別にリセットできます。

詳細設定ウィンドウで各項目の上部にあるリセットボタンをクリックすると設定をリセットできます・

以下の例では、ライトの設定を初期状態にリセットしています。

<div class="row">
{% include docimg.html file="/images/docs/reset_setting_before.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/reset_setting_after.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>
